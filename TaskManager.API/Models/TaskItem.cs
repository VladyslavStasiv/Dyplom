// 💡 ДОДАНО: Цей рядок підключає інструменти для роботи з JSON
using System.Text.Json.Serialization;

namespace TaskManager.API.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Поля для твого алгоритму черговості (дипломна фішка)
        public DateTime? Deadline { get; set; }
        public int Complexity { get; set; } // Оцінка складності (наприклад, 1-10)
        public double PriorityScore { get; set; } // Розрахований пріоритет (чим більше, тим важливіше)
        
        // Зв'язок з колонкою на дошці
        public int ColumnId { get; set; }

        // 💡 ДОДАНО: Кажемо генератору ігнорувати цей об'єкт при створенні JSON
        [JsonIgnore]
        public BoardColumn? Column { get; set; }

        // 💡 НОВЕ: Зв'язок з користувачем (власником задачі)
        // Саме ці два поля гарантують, що кожен бачить ТІЛЬКИ свої задачі
        public int UserId { get; set; }

        // 💡 ДОДАНО: Кажемо генератору ігнорувати цей об'єкт при створенні JSON
        [JsonIgnore]
        public User? User { get; set; }
    }
}