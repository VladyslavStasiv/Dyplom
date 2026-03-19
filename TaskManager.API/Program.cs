using Microsoft.EntityFrameworkCore;
using TaskManager.API.Data;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// 1. Підключення до бази даних PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 💡 НОВЕ: Налаштовуємо CORS (дозволяємо Angular підключатися до API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Це стандартний порт, на якому запуститься Angular
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 2. Додаємо підтримку Контролерів
builder.Services.AddControllers();

// --- ДОДАЙ ЦЕЙ БЛОК ДЛЯ АВТОРИЗАЦІЇ ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// 3. Вбудована підтримка OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// 💡 НОВЕ: Вмикаємо нашого "охоронця" CORS (обов'язково ПЕРЕД Authorization)
app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- ДОДАЄМО СТАНДАРТНІ КОЛОНКИ ДЛЯ РОБОТИ АНГЮЛЯРА ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TaskManager.API.Data.AppDbContext>();

    // 1. Створюємо системного користувача (щоб дошка мала легального власника)
    var systemUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "system@admin.com");
    if (systemUser == null)
    {
        systemUser = new TaskManager.API.Models.User 
    { 
        Username = "SystemAdmin", 
        Email = "system@admin.com", 
        // Використовуємо генератор випадкових рядків, щоб аналізатор був спокійний
        PasswordHash = Guid.NewGuid().ToString() 
    };
        context.Users.Add(systemUser);
        await context.SaveChangesAsync();
    }

    // 2. Створюємо Дошку і робимо Систему її власником
    var board = await context.Boards.FirstOrDefaultAsync();
    if (board == null)
    {
        board = new TaskManager.API.Models.Board { Title = "Головна дошка", OwnerId = systemUser.Id }; 
        context.Boards.Add(board);
        await context.SaveChangesAsync(); 
    }

    // 3. Створюємо 3 стандартні колонки (ID 1, 2, 3) для Angular
    if (!await context.BoardColumns.AnyAsync()) 
    {
        context.BoardColumns.AddRange(
            new TaskManager.API.Models.BoardColumn { Title = "До виконання", Position = 1, BoardId = board.Id },
            new TaskManager.API.Models.BoardColumn { Title = "В процесі", Position = 2, BoardId = board.Id },
            new TaskManager.API.Models.BoardColumn { Title = "Готово", Position = 3, BoardId = board.Id }
        );
        await context.SaveChangesAsync(); 
    }
}
// --------------------------------------------------------

await app.RunAsync();