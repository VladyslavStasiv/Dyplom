namespace TaskManager.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // Дошки, які створив цей користувач (він є власником)
        public List<Board> OwnedBoards { get; set; } = new List<Board>();

        // Дошки, до яких користувачу дали доступ інші люди
        public List<UserBoard> SharedBoards { get; set; } = new List<UserBoard>();
    }
}