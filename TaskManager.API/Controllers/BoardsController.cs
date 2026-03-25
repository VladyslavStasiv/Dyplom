using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Data;
using TaskManager.API.Models;
using TaskManager.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BoardsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BoardsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/boards
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Board>>> GetBoards()
        {
            // 1. Дістаємо ID користувача, який зараз робить запит (з його JWT токена)
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Користувача не розпізнано.");
            }
            int currentUserId = int.Parse(userIdString);

            // 💡 ОНОВЛЕНО: Шукаємо дошки і підтягуємо таблицю доступів, щоб знати РОЛІ!
            var userBoards = await _context.Boards
                .Include(b => b.Columns) 
                .Include(b => b.Owner)   
                .Include(b => b.SharedWithUsers) // 🚀 ДОДАЛИ ЦЕ (для ролей)
                    .ThenInclude(ub => ub.User)  // 🚀 І ЦЕ (для ролей)
                .Where(b => b.OwnerId == currentUserId || b.SharedWithUsers.Any(ub => ub.UserId == currentUserId))
                .ToListAsync();

            return Ok(userBoards);
        }

        // POST: api/boards
        [HttpPost]
        public async Task<ActionResult<Board>> CreateBoard(Board board)
        {
            _context.Boards.Add(board);
            await _context.SaveChangesAsync();

            return Ok(board);
        }

        // 💡 НОВИЙ МЕТОД: Надання спільного доступу до дошки
        // POST: api/boards/share
        [HttpPost("share")]
        public async Task<IActionResult> ShareBoard([FromBody] ShareBoardDto request)
        {
            // 1. Шукаємо користувача за Email
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.UserEmail);
            if (targetUser == null) 
            {
                return NotFound(new { message = "Користувача з таким Email не знайдено." });
            }

            // 2. Перевіряємо, чи існує сама дошка
            var board = await _context.Boards.FindAsync(request.BoardId);
            if (board == null) 
            {
                return NotFound(new { message = "Дошку не знайдено." });
            }

            // 3. Перевіряємо, чи ми вже не давали доступ цій людині (щоб не було дублікатів)
            var alreadyShared = await _context.UserBoards
                .AnyAsync(ub => ub.UserId == targetUser.Id && ub.BoardId == request.BoardId);
            
            if (alreadyShared) 
            {
                return BadRequest(new { message = "Цей користувач вже має доступ до цієї дошки." });
            }

            // 4. Створюємо зв'язок у нашій таблиці-мосту!
            var userBoard = new UserBoard
            {
                UserId = targetUser.Id,
                BoardId = request.BoardId,
                AccessLevel = request.AccessLevel
            };

            _context.UserBoards.Add(userBoard);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Доступ успішно надано користувачу {targetUser.Username}!" });
        }

        // 1. Отримати список користувачів, які мають доступ до дошки
        [HttpGet("{boardId}/shared-users")]
        public async Task<IActionResult> GetSharedUsers(int boardId)
        {
            // Дізнаємося, хто зараз робить запит
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Шукаємо дошку і одразу підтягуємо проміжну таблицю та самих юзерів
            var board = await _context.Boards
                .Include(b => b.SharedWithUsers)
                    .ThenInclude(ub => ub.User)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null) return NotFound(new { message = "Дошку не знайдено" });

            // БЕЗПЕКА: Тільки власник дошки має право бачити, кому він дав доступ
            if (board.OwnerId != currentUserId) return Forbid();

            // Формуємо гарну відповідь (щоб не віддавати паролі та зайві дані)
            var sharedUsers = board.SharedWithUsers.Select(ub => new 
            {
                id = ub.User!.Id,
                username = ub.User.Username,
                email = ub.User.Email,
                accessLevel = ub.AccessLevel
            });

            return Ok(sharedUsers);
        }

        // 2. Забрати доступ у користувача
        [HttpDelete("{boardId}/share/{userId}")]
        public async Task<IActionResult> RevokeAccess(int boardId, int userId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var board = await _context.Boards.FindAsync(boardId);
            if (board == null) return NotFound(new { message = "Дошку не знайдено" });

            // БЕЗПЕКА: Тільки власник може викидати людей з дошки
            if (board.OwnerId != currentUserId) return Forbid();

            // Шукаємо запис у проміжній таблиці
            var userBoard = await _context.UserBoards
                .FirstOrDefaultAsync(ub => ub.BoardId == boardId && ub.UserId == userId);

            if (userBoard == null) return NotFound(new { message = "Цей користувач не має доступу до дошки" });

            // Видаляємо зв'язок
            _context.UserBoards.Remove(userBoard);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Доступ успішно скасовано!" });
        }

        // DELETE: api/boards/5
        [HttpDelete("{id}")]
        [AllowAnonymous] // 💡 Дозволяємо тимчасово видалити без токена, бо ми не можемо залогінитися під Адміном
        public async Task<IActionResult> DeleteBoard(int id)
        {
            var board = await _context.Boards.FindAsync(id);
            
            if (board == null)
            {
                return NotFound(new { message = "Дошку не знайдено" });
            }

            _context.Boards.Remove(board);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Дошку успішно видалено!" });
        }
    }
}