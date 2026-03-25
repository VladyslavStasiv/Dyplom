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
        
        // 💡 НОВЕ: Робимо пріоритет динамічним!
        [NotMapped] // Цей атрибут каже Entity Framework НЕ створювати колонку в базі даних
        public double PriorityScore 
        { 
            get 
            {
                double score = Complexity * 10; 

                if (Deadline.HasValue)
                {
                    var daysLeft = (Deadline.Value - DateTime.UtcNow).TotalDays;
                    
                    if (daysLeft > 0)
                    {
                        score += (100 / daysLeft); 
                    }
                    else
                    {
                        score += 1000; 
                    }
                }
                
                return Math.Round(score, 2);
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