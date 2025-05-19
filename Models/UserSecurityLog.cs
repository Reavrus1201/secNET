using System;

namespace secNET.Models
{
    public class UserSecurityLog
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public DateTime ResetDateTime { get; set; }
        public string AdminUsername { get; set; }
        public string NewPassword { get; set; }
    }
}