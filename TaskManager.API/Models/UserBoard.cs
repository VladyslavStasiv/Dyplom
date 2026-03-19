namespace TaskManager.API.Models
{
    // Це таблиця-міст, яка з'єднує Користувача і Дошку для спільного доступу
    public class UserBoard
    {
        public int UserId { get; set; }
        public User? User { get; set; }

        public int BoardId { get; set; }
        public Board? Board { get; set; }

        // Рівень доступу: наприклад "Viewer" (тільки читати) або "Editor" (редагувати)
        public string AccessLevel { get; set; } = "Editor"; 
    }
}