namespace FileHandlingBackend.Models
{
    public class Model
    {
        public int Id { get; set; }
        public string? S3Key { get; set; }
        public string? TagString { get; set; }
        public string? Description { get; set; }
        public DateTime TimeStamp { get; set; }
        public required string Title { get; set; }
        public bool IsPublic { get; set; } = false;
        public int UserId { get; set; }
    }
}
