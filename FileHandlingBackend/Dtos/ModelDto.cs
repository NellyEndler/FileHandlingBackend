namespace FileHandlingBackend.Dtos
{
    public class ModelDto
    {
        public int Id { get; set; }
        public List<string>? TagNames { get; set; }
        public string TagString { get; set; }
        public string? Description { get; set; }
        public string? S3Key { get; set; }
        public required string Title { get; set; }
        public bool IsPublic { get; set; } = false;
        public DateTime TimeStamp { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
    }

    public class UpdateModelDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? TagString { get; set; }
    }

    public class WebModelDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? TagString { get; set; }
        public string ModelGlbUrl { get; set; }
        public bool IsPublic { get; set; }
        public string Username { get; set; }
    }

    public class CreateFullModelDto
    {
        public IFormFile ModelFile { get; set; }
        public string Title { get; set; }
        public string? TagString { get; set; }
        public string? Description { get; set; }
    }
}
