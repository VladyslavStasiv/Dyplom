using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TaskManager.API.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        public DateTime? Deadline { get; set; }
        public int Complexity { get; set; } 
        
        // 💡 НОВЕ: Багатокритеріальний зважений алгоритм пріоритезації
        [NotMapped] 
public int PriorityScore 
{ 
    get 
    {
        int score = 0;

        // 1. Оцінка складності (Складність 1-10 * 3) -> Макс 30 балів
        score += Complexity * 3; 

        if (Deadline.HasValue)
        {
            var timeRemaining = Deadline.Value - DateTime.UtcNow;
            
            if (timeRemaining.TotalHours < 0)
            {
                // 💡 НОВА ЛОГІКА: Базовий штраф 70 + 5 балів за кожен день прострочення
                int daysOverdue = (int)Math.Abs(timeRemaining.TotalDays);
                score += 70 + (daysOverdue * 5); 
            }
            else if (timeRemaining.TotalHours <= 24)
            {
                score += 50; // Менше доби
            }
            else if (timeRemaining.TotalDays <= 3)
            {
                score += 30; // Менше 3 днів
            }
            else if (timeRemaining.TotalDays <= 7)
            {
                score += 15; // Менше тижня
            }
        }
        
        return score;
    }
}
        
        public int ColumnId { get; set; }

        [JsonIgnore]
        public BoardColumn? Column { get; set; }

        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }
    }
}