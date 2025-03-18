namespace FileHandlingBackend.Dtos
{
    public class S3ObjectDto
    {
        public string? Name { get; set; }
        public DateTime? LastModified { get; set; }
        public string? PresignedUrl { get; set; }
    }
}
