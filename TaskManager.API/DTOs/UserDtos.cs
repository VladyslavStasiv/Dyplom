namespace TaskManager.API.DTOs
{
    // Цей конверт ми будемо отримувати від Angular при реєстрації
    public class UserRegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // А цей конверт — при спробі увійти в систему (логін)
    public class UserLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}