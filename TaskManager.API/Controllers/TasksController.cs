using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.API.Data;
using TaskManager.API.Models;

namespace TaskManager.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // 💡 ДОПОМІЖНИЙ МЕТОД: Записує подію в базу
        private async Task LogActivity(int boardId, int userId, string action)
        {
            var activity = new BoardActivity
            {
                BoardId = boardId,
                UserId = userId,
                ActionDescription = action,
                Timestamp = DateTime.UtcNow
            };
            _context.BoardActivities.Add(activity);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            var userId = GetCurrentUserId(); 

            var accessibleBoardIds = await _context.Boards
                .Where(b => b.OwnerId == userId || (b.SharedWithUsers != null && b.SharedWithUsers.Any(ub => ub.UserId == userId)))
                .Select(b => b.Id)
                .ToListAsync();

            var tasks = await _context.Tasks
                .Include(t => t.Column) 
                .Where(t => t.Column != null && accessibleBoardIds.Contains(t.Column!.BoardId))
                .ToListAsync();

            var sortedTasks = tasks
                .OrderByDescending(t => t.PriorityScore)
                .ToList();

            return Ok(sortedTasks);
        }

        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItem task)
        {
            var currentUserId = GetCurrentUserId();

            var board = await _context.Boards
                .Include(b => b.SharedWithUsers)
                .FirstOrDefaultAsync(b => b.Columns != null && b.Columns.Any(c => c.Id == task.ColumnId));

            if (board == null) return BadRequest("Дошку або колонку не знайдено.");

            bool isBoardOwner = board.OwnerId == currentUserId;
            bool isBoardEditor = board.SharedWithUsers != null && board.SharedWithUsers.Any(ub => ub.UserId == currentUserId && ub.AccessLevel == "Editor");

            if (!isBoardOwner && !isBoardEditor) return Forbid(); 

            task.UserId = currentUserId;

            _context.Tasks.Add(task);
            
            // 💡 ІСТОРІЯ: Фіксуємо створення
            await LogActivity(board.Id, currentUserId, $"створив(ла) задачу '{task.Title}'");

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem updatedTask)
        {
            if (id != updatedTask.Id) return BadRequest("ID задачі не співпадає.");

            var currentUserId = GetCurrentUserId();

            var existingTask = await _context.Tasks
                .Include(t => t.Column)
                    .ThenInclude(c => c!.Board)
                        .ThenInclude(b => b!.SharedWithUsers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (existingTask == null) return NotFound();

            bool isTaskCreator = existingTask.UserId == currentUserId;
            bool isBoardOwner = false;
            bool isBoardEditor = false;
            int boardId = existingTask.Column!.BoardId; 

            if (existingTask.Column != null && existingTask.Column.Board != null)
            {
                isBoardOwner = existingTask.Column.Board.OwnerId == currentUserId;
                
                if (existingTask.Column.Board.SharedWithUsers != null)
                {
                    isBoardEditor = existingTask.Column.Board.SharedWithUsers.Any(ub => ub.UserId == currentUserId && ub.AccessLevel == "Editor");
                }
            }

            if (!isTaskCreator && !isBoardOwner && !isBoardEditor) return Forbid(); 

            // 💡 ІСТОРІЯ: Визначаємо, що саме змінилося (переміщення чи редагування)
            string actionDetail = $"оновив(ла) задачу '{updatedTask.Title}'";
            if (existingTask.ColumnId != updatedTask.ColumnId)
            {
                // Якщо змінилась колонка - значить задачу перетягнули
                var newColumn = await _context.BoardColumns.FindAsync(updatedTask.ColumnId);
                actionDetail = $"перемістив(ла) задачу '{updatedTask.Title}' у колонку '{newColumn?.Title}'";
            }

            existingTask.Title = updatedTask.Title;
            existingTask.Description = updatedTask.Description;
            existingTask.Deadline = updatedTask.Deadline;
            existingTask.Complexity = updatedTask.Complexity;
            existingTask.ColumnId = updatedTask.ColumnId;
            
            await LogActivity(boardId, currentUserId, actionDetail);

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return StatusCode(500, "Помилка при оновленні бази даних."); }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var currentUserId = GetCurrentUserId();

            var task = await _context.Tasks
                .Include(t => t.Column)
                    .ThenInclude(c => c!.Board)
                        .ThenInclude(b => b!.SharedWithUsers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            bool isTaskCreator = task.UserId == currentUserId;
            bool isBoardOwner = false;
            bool isBoardEditor = false;
            int boardId = task.Column!.BoardId;

            if (task.Column != null && task.Column.Board != null)
            {
                isBoardOwner = task.Column.Board.OwnerId == currentUserId;

                if (task.Column.Board.SharedWithUsers != null)
                {
                    isBoardEditor = task.Column.Board.SharedWithUsers.Any(ub => ub.UserId == currentUserId && ub.AccessLevel == "Editor");
                }
            }

            if (!isTaskCreator && !isBoardOwner && !isBoardEditor) return Forbid(); 

            // 💡 ІСТОРІЯ: Фіксуємо видалення
            await LogActivity(boardId, currentUserId, $"видалив(ла) задачу '{task.Title}'");

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 💡 НОВИЙ МЕТОД: Отримання історії для конкретної дошки
        [HttpGet("history/{boardId}")]
        public async Task<IActionResult> GetBoardHistory(int boardId)
        {
            var currentUserId = GetCurrentUserId();

            // Перевіряємо, чи має юзер доступ до дошки (щоб чужі не підглядали)
            var board = await _context.Boards
                .Include(b => b.SharedWithUsers)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null) return NotFound("Дошку не знайдено");

            bool hasAccess = board.OwnerId == currentUserId || 
                            (board.SharedWithUsers != null && board.SharedWithUsers.Any(ub => ub.UserId == currentUserId));

            if (!hasAccess) return Forbid();

            // Повертаємо останні 50 дій
            var history = await _context.BoardActivities
                .Include(a => a.User)
                .Where(a => a.BoardId == boardId)
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .Select(a => new {
                    username = a.User!.Username,
                    action = a.ActionDescription,
                    timestamp = a.Timestamp
                })
                .ToListAsync();

            return Ok(history);
        }
    }
}