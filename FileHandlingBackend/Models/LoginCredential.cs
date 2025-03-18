namespace FileHandlingBackend.Models
{
    public class LoginCredential
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string UserName { get; set; }
        public required string HashedPassword { get; set; }
        public DateTime LastLogin { get; set; }
    }
}
