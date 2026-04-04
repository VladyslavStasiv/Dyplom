namespace TaskManager.API.Models
{
    public class UserBoard
    {
        // 💡 НОВЕ: Створюємо константи, щоб SonarLint був щасливий
        public const string StatusPending = "Pending";
        public const string StatusAccepted = "Accepted";

        public int UserId { get; set; }
        public User? User { get; set; }

        public int BoardId { get; set; }
        public Board? Board { get; set; }

        public string AccessLevel { get; set; } = "Editor"; 

        // Використовуємо константу за замовчуванням
        public string InvitationStatus { get; set; } = StatusPending;
    }
}