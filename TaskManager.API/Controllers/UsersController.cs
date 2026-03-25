using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManager.API.Data;
using TaskManager.API.Models;
using TaskManager.API.DTOs; // Підключили наші конверти

namespace TaskManager.API.Controllers
{
    // Вказуємо шлях, за яким буде доступний цей контролер
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // Додали для роботи з токенами

        // Підключаємо нашу базу даних та конфігурацію через конструктор
        public UsersController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users
                .Include(u => u.OwnedBoards)    
                .Include(u => u.SharedBoards)   
                .ToListAsync();
        }

        // POST: api/users/register (БЕЗПЕЧНА РЕЄСТРАЦІЯ замість старого CreateUser)
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserRegisterDto request)
        {
            // Перевіряємо, чи немає вже такого email в базі
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Користувач з таким Email вже існує.");
            }

            // Хешуємо пароль (тепер він виглядає як складний набір символів)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash
            };

            _context.Users.Add(user);
            // 💡 ЗБЕРІГАЄМО КОРИСТУВАЧА ПЕРШИМ, щоб база даних видала йому унікальний ID
            await _context.SaveChangesAsync(); 

            // 💡 НОВИЙ БЛОК: Автоматично створюємо персональну дошку для новачка
            var defaultBoard = new Board
            {
                Title = "Головна дошка", // Або $"Дошка {user.Username}"
                OwnerId = user.Id,
                Columns = new List<BoardColumn>
                {
                    new BoardColumn { Title = "До виконання", Position = 1 },
                    new BoardColumn { Title = "В процесі", Position = 2 },
                    new BoardColumn { Title = "Готово", Position = 3 }
                }
            };

            _context.Boards.Add(defaultBoard);
            // Зберігаємо дошку разом із колонками
            await _context.SaveChangesAsync();

            return Ok(new { message = "Реєстрація успішна! Персональну дошку створено." });
        }

        // POST: api/users/login (Вхід в систему та отримання токена)
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            // Якщо користувача немає або пароль не збігається з хешем
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Неправильний Email або пароль.");
            }

            // Якщо все ок - генеруємо токен і віддаємо його на фронтенд
            string token = CreateToken(user);
            return Ok(new { token = token }); 
        }

        // DELETE: api/users/5 (Видалити користувача за його ID)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // Шукаємо користувача в базі за ID
            var user = await _context.Users.FindAsync(id);
            
            // Якщо такого немає, повертаємо помилку 404
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Допоміжний метод для створення самого JWT-токена
        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            // Беремо секретний ключ з файлу налаштувань appsettings.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1), // Токен дійсний 1 день
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}