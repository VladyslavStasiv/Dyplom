namespace TaskManager.API.DTOs
{
    public class ShareBoardDto
    {
        public int BoardId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = "Editor";
    }
}