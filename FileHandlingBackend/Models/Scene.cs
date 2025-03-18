namespace FileHandlingBackend.Models
{
    public class Scene
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? S3Key { get; set; }
        public string? SlideIds { get; set; }
        public DateTime CreatedDate { get; set; }
        public int OwnerId { get; set; }
    }
}
