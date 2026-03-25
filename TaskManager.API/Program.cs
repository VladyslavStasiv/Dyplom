using Microsoft.EntityFrameworkCore;
using TaskManager.API.Data;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

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
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Цей магічний рядок каже: "Якщо бачиш нескінченний цикл - просто зупинись і не видавай помилку"
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

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

await app.RunAsync();