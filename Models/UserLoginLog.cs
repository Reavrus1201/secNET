using System;

namespace secNET.Models
{
    public class UserLoginLog
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public DateTime LoginDateTime { get; set; }
        public int BranchId { get; set; }
        public DateTime? LogoutDateTime { get; set; }
    }
}