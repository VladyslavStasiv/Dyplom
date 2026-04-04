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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Board>>> GetBoards()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized("Користувача не розпізнано.");
            
            int currentUserId = int.Parse(userIdString);

            var userBoards = await _context.Boards
                .Include(b => b.Columns) 
                .Include(b => b.Owner)   
                .Include(b => b.SharedWithUsers) 
                    .ThenInclude(ub => ub.User)  
                .Where(b => b.OwnerId == currentUserId || 
                            b.SharedWithUsers.Any(ub => ub.UserId == currentUserId && ub.InvitationStatus == UserBoard.StatusAccepted))
                .ToListAsync();

            return Ok(userBoards);
        }

        [HttpPost]
        public async Task<ActionResult<Board>> CreateBoard(Board board)
        {
            _context.Boards.Add(board);
            await _context.SaveChangesAsync();
            return Ok(board);
        }

        [HttpPost("share")]
        public async Task<IActionResult> ShareBoard([FromBody] ShareBoardDto request)
        {
            // 💡 1. Дізнаємося, хто саме зараз робить запит
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.UserEmail);
            if (targetUser == null) return NotFound(new { message = "Користувача з таким Email не знайдено." });

            var board = await _context.Boards.FindAsync(request.BoardId);
            if (board == null) return NotFound(new { message = "Дошку не знайдено." });

            // 💡 2. ГОЛОВНИЙ ЗАХИСТ: Перевіряємо, чи є цей юзер ВЛАСНИКОМ дошки
            if (board.OwnerId != currentUserId)
            {
                return BadRequest(new { message = "Помилка доступу: Тільки власник може запрошувати інших користувачів на цю дошку." });
            }

            if (board.OwnerId == targetUser.Id)
            {
                return BadRequest(new { message = "Ви є власником цієї дошки. Вам не потрібно надсилати запрошення самому собі!" });
            }

            var alreadyShared = await _context.UserBoards
                .FirstOrDefaultAsync(ub => ub.UserId == targetUser.Id && ub.BoardId == request.BoardId);
            
            if (alreadyShared != null) 
            {
                if (alreadyShared.InvitationStatus == UserBoard.StatusPending)
                    return BadRequest(new { message = "Цей користувач вже має активне запрошення." });
                else
                    return BadRequest(new { message = "Цей користувач вже має доступ до цієї дошки." });
            }

            var userBoard = new UserBoard
            {
                UserId = targetUser.Id,
                BoardId = request.BoardId,
                AccessLevel = request.AccessLevel,
                InvitationStatus = UserBoard.StatusPending 
            };

            _context.UserBoards.Add(userBoard);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Запрошення успішно надіслано користувачу {targetUser.Username}!" });
        }

        [HttpGet("invitations")]
        public async Task<IActionResult> GetPendingInvitations()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var invitations = await _context.UserBoards
                .Include(ub => ub.Board)
                    .ThenInclude(b => b!.Owner)
                .Where(ub => ub.UserId == currentUserId && ub.InvitationStatus == UserBoard.StatusPending)
                .Select(ub => new {
                    BoardId = ub.BoardId,
                    BoardTitle = ub.Board!.Title,
                    OwnerName = ub.Board.Owner!.Username,
                    AccessLevel = ub.AccessLevel
                })
                .ToListAsync();

            return Ok(invitations);
        }

        [HttpPost("{boardId}/accept")]
        public async Task<IActionResult> AcceptInvitation(int boardId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var invitation = await _context.UserBoards
                .FirstOrDefaultAsync(ub => ub.BoardId == boardId && ub.UserId == currentUserId && ub.InvitationStatus == UserBoard.StatusPending);

            if (invitation == null) return NotFound(new { message = "Запрошення не знайдено." });

            invitation.InvitationStatus = UserBoard.StatusAccepted;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Запрошення прийнято!" });
        }

        [HttpPost("{boardId}/decline")]
        public async Task<IActionResult> DeclineInvitation(int boardId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var invitation = await _context.UserBoards
                .FirstOrDefaultAsync(ub => ub.BoardId == boardId && ub.UserId == currentUserId && ub.InvitationStatus == UserBoard.StatusPending);

            if (invitation == null) return NotFound(new { message = "Запрошення не знайдено." });

            _context.UserBoards.Remove(invitation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Запрошення відхилено." });
        }

        [HttpGet("{boardId}/shared-users")]
        public async Task<IActionResult> GetSharedUsers(int boardId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var board = await _context.Boards
                .Include(b => b.SharedWithUsers)
                    .ThenInclude(ub => ub.User)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null) return NotFound(new { message = "Дошку не знайдено" });
            if (board.OwnerId != currentUserId) return Forbid();

            var sharedUsers = board.SharedWithUsers.Select(ub => new 
            {
                id = ub.User!.Id,
                username = ub.User.Username,
                email = ub.User.Email,
                accessLevel = ub.AccessLevel,
                status = ub.InvitationStatus 
            });

            return Ok(sharedUsers);
        }

        [HttpDelete("{boardId}/share/{userId}")]
        public async Task<IActionResult> RevokeAccess(int boardId, int userId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var board = await _context.Boards.FindAsync(boardId);
            if (board == null) return NotFound(new { message = "Дошку не знайдено" });
            if (board.OwnerId != currentUserId) return Forbid();

            var userBoard = await _context.UserBoards
                .FirstOrDefaultAsync(ub => ub.BoardId == boardId && ub.UserId == userId);

            if (userBoard == null) return NotFound(new { message = "Цей користувач не має доступу до дошки" });

            _context.UserBoards.Remove(userBoard);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Доступ успішно скасовано!" });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var board = await _context.Boards.FindAsync(id);
            if (board == null) return NotFound(new { message = "Дошку не знайдено" });

            if (board.OwnerId != currentUserId) return Forbid();

            _context.Boards.Remove(board);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Дошку успішно видалено!" });
        }
    }
}