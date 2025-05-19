namespace secNET.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public int TierLevel { get; set; }
        public int? BranchId { get; set; } // Changed from int to int?
        public Branch Branch { get; set; }
        public string HashType { get; set; }
    }
}