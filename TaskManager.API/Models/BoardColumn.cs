namespace TaskManager.API.Models
{
    public class BoardColumn
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Position { get; set; } // Порядок колонки (1, 2, 3...)
        
        public int BoardId { get; set; }
        public Board? Board { get; set; }

        // Зв'язок: в одній колонці багато задач
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}