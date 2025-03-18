namespace FileHandlingBackend.Dtos
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int OwnerId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateProjectDto
    {
        public required string Name { get; set; }
        public string? CollaboratorIds { get; set; }
        public string? SceneIds { get; set; }
    }
}
