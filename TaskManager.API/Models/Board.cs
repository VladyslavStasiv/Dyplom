namespace TaskManager.API.Models
{
    public class Board
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        // Вказуємо, хто створив цю дошку (Власник)
        public int OwnerId { get; set; }
        public User? Owner { get; set; }

        // Колонки на цій дошці
        public List<BoardColumn> Columns { get; set; } = new List<BoardColumn>();

        // Список людей, яким власник дав доступ до цієї дошки
        public List<UserBoard> SharedWithUsers { get; set; } = new List<UserBoard>();
    }
}