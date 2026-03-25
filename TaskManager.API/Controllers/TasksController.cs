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

        // GET: api/tasks 
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            var userId = GetCurrentUserId(); 

            // 💡 ВІДШЛІФОВАНО: Додано явну перевірку `!= null` у запит до бази
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

        // POST: api/tasks
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

            if (!isBoardOwner && !isBoardEditor)
            {
                return Forbid(); 
            }

            task.UserId = currentUserId;

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(task);
        }

        // PUT: api/tasks/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem updatedTask)
        {
            if (id != updatedTask.Id) return BadRequest("ID задачі не співпадає.");

            var currentUserId = GetCurrentUserId();

            // 💡 ВІДШЛІФОВАНО: Додано `!` для методів Include, щоб компілятор не хвилювався
            var existingTask = await _context.Tasks
                .Include(t => t.Column)
                    .ThenInclude(c => c!.Board)
                        .ThenInclude(b => b!.SharedWithUsers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (existingTask == null) return NotFound();

            // 💡 ВІДШЛІФОВАНО: Залізобетонна перевірка без використання `?.`
            bool isTaskCreator = existingTask.UserId == currentUserId;
            bool isBoardOwner = false;
            bool isBoardEditor = false;

            if (existingTask.Column != null && existingTask.Column.Board != null)
            {
                isBoardOwner = existingTask.Column.Board.OwnerId == currentUserId;
                
                if (existingTask.Column.Board.SharedWithUsers != null)
                {
                    isBoardEditor = existingTask.Column.Board.SharedWithUsers.Any(ub => ub.UserId == currentUserId && ub.AccessLevel == "Editor");
                }
            }

            if (!isTaskCreator && !isBoardOwner && !isBoardEditor)
            {
                return Forbid(); 
            }

            existingTask.Title = updatedTask.Title;
            existingTask.Description = updatedTask.Description;
            existingTask.Deadline = updatedTask.Deadline;
            existingTask.Complexity = updatedTask.Complexity;
            existingTask.ColumnId = updatedTask.ColumnId;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Помилка при оновленні бази даних.");
            }

            return NoContent();
        }

        // DELETE: api/tasks/5
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

            // 💡 ВІДШЛІФОВАНО: Класична перевірка замість магічних символів
            if (task.Column != null && task.Column.Board != null)
            {
                isBoardOwner = task.Column.Board.OwnerId == currentUserId;

                if (task.Column.Board.SharedWithUsers != null)
                {
                    isBoardEditor = task.Column.Board.SharedWithUsers.Any(ub => ub.UserId == currentUserId && ub.AccessLevel == "Editor");
                }
            }

            if (!isTaskCreator && !isBoardOwner && !isBoardEditor)
            {
                return Forbid(); 
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}