using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.API.Data;
using TaskManager.API.Models;

namespace TaskManager.API.Controllers
{
    // Атрибут [Authorize] означає, що сюди можуть звертатися ТІЛЬКИ ті, хто має дійсний токен.
    // Без токена сервер навіть не буде читати код далі і відразу поверне помилку 401.
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

        // 💡 СУПЕР ВАЖЛИВИЙ МЕТОД: дістає ID поточного користувача прямо з його токена
        private int GetCurrentUserId()
        {
            // Angular надсилає токен у кожному запиті. Ми беремо з нього NameIdentifier (це наш UserId)
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // GET: api/tasks (Отримати всі задачі ПОТОЧНОГО користувача)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            var userId = GetCurrentUserId(); // Дізнаємося, хто саме зараз робить запит

            return await _context.Tasks
                .Where(t => t.UserId == userId) // ФІЛЬТР: витягуємо з бази тільки задачі цього юзера!
                .OrderByDescending(t => t.PriorityScore) // Сортуємо за пріоритетністю
                .ToListAsync();
        }

        // POST: api/tasks (Створити нову задачу)
        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItem task)
        {
            // Прив'язуємо новостворену задачу до того, хто зараз залогінений
            task.UserId = GetCurrentUserId();

            // Рахуємо пріоритет за твоїм алгоритмом
            task.PriorityScore = CalculatePriority(task.Deadline, task.Complexity);

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(task);
        }

        // Твій алгоритм розрахунку пріоритету
        private static double CalculatePriority(DateTime? deadline, int complexity)
        {
            double score = complexity * 10; // Базові бали за складність

            if (deadline.HasValue)
            {
                var daysLeft = (deadline.Value - DateTime.UtcNow).TotalDays;
                
                if (daysLeft > 0)
                {
                    score += (100 / daysLeft); // Чим ближче дедлайн, тим більше додаємо балів
                }
                else
                {
                    score += 1000; // Прострочена задача отримує максимальний пріоритет
                }
            }
            
            return Math.Round(score, 2);
        }

        // PUT: api/tasks/5 (Оновити існуючу задачу)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem updatedTask)
        {
            if (id != updatedTask.Id) return BadRequest("ID задачі не співпадає.");

            var existingTask = await _context.Tasks.FindAsync(id);
            if (existingTask == null) return NotFound();

            // ⚠️ ПЕРЕВІРКА БЕЗПЕКИ: чи дійсно ця задача належить цьому користувачу?
            // Щоб хакер не міг змінити чужу задачу, просто вгадавши її ID.
            if (existingTask.UserId != GetCurrentUserId())
            {
                return Forbid(); // Повертаємо 403 Forbidden (Доступ заборонено)
            }

            // Оновлюємо дані...
            existingTask.Title = updatedTask.Title;
            existingTask.Description = updatedTask.Description;
            existingTask.Deadline = updatedTask.Deadline;
            existingTask.Complexity = updatedTask.Complexity;
            existingTask.ColumnId = updatedTask.ColumnId;
            
            // Перераховуємо пріоритет (дедлайн чи складність могли змінитися)
            existingTask.PriorityScore = CalculatePriority(existingTask.Deadline, existingTask.Complexity);

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

        // DELETE: api/tasks/5 (Видалити задачу)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            // ⚠️ ПЕРЕВІРКА БЕЗПЕКИ: видаляти можна тільки СВОЇ задачі
            if (task.UserId != GetCurrentUserId())
            {
                return Forbid(); 
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}