using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Data;
using TaskManager.API.Models;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            // Include додає пов'язані колонки до відповіді (корисно для фронтенду)
            return await _context.Boards.Include(b => b.Columns).ToListAsync();
        }

        // POST: api/boards
        [HttpPost]
        public async Task<ActionResult<Board>> CreateBoard(Board board)
        {
            _context.Boards.Add(board);
            await _context.SaveChangesAsync();

            return Ok(board);
        }
    }
}