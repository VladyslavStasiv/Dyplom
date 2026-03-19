using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Data;
using TaskManager.API.Models;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoardColumnsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BoardColumnsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/boardcolumns
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BoardColumn>>> GetColumns()
        {
            return await _context.BoardColumns.Include(c => c.Tasks).ToListAsync();
        }

        // POST: api/boardcolumns
        [HttpPost]
        public async Task<ActionResult<BoardColumn>> CreateColumn(BoardColumn column)
        {
            _context.BoardColumns.Add(column);
            await _context.SaveChangesAsync();

            return Ok(column);
        }
    }
}