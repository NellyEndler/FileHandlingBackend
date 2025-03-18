namespace FileHandlingBackend.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int OwnerId { get; set; }
        public string? CollaboratorIds { get; set; }
        public string? SceneIds { get; set; }
        public DateTime CreatedDate { get; set; }
        public int LastEditedById { get; set; }
        public DateTime? LastEdited { get; set; }
    }
}
