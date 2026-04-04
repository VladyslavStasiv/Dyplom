namespace TaskManager.API.Models
{
    public class BoardActivity
    {
        public int Id { get; set; }
        
        // Дошка, на якій відбулася дія
        public int BoardId { get; set; }
        public Board? Board { get; set; }

        // Користувач, який виконав дію
        public int UserId { get; set; }
        public User? User { get; set; }

        // Текст події (наприклад: "Створив задачу 'Написати звіт'")
        public string ActionDescription { get; set; } = string.Empty;

        // Час події
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}