namespace FileHandlingBackend.Models
{
    public class Token
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string StringToken { get; set; }
        public DateTime Expires { get; set; }
    }
}
