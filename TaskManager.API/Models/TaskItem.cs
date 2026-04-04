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

                // 1. Оцінка складності (Складність від 1 до 10 множимо на 3)
                // Максимум: 30 балів
                score += Complexity * 3; 

                // 2. Оцінка дедлайну (Експоненційне зростання важливості)
                if (Deadline.HasValue)
                {
                    // Використовуємо UtcNow, щоб не було проблем з часовими поясами
                    var timeRemaining = Deadline.Value - DateTime.UtcNow;
                    
                    if (timeRemaining.TotalHours < 0)
                    {
                        score += 70; // 🚨 Прострочено: Максимальний пріоритет
                    }
                    else if (timeRemaining.TotalHours <= 24)
                    {
                        score += 50; // ⏳ Менше доби до здачі
                    }
                    else if (timeRemaining.TotalDays <= 3)
                    {
                        score += 30; // 📅 Менше 3 днів
                    }
                    else if (timeRemaining.TotalDays <= 7)
                    {
                        score += 15; // 🗓️ Менше тижня
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