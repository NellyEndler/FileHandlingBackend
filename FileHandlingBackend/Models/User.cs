namespace FileHandlingBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public int RoleId { get; set; }
        public int? CompanyId { get; set; }
        public DateTime? JoinedDate { get; set; }
    }
}
