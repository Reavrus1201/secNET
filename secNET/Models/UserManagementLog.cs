using System;

namespace secNET.Models
{
    public class UserManagementLog
    {
        public int Id { get; set; }
        public string ActionType { get; set; } // 'New' or 'Deleted'
        public string Username { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public string AdminUsername { get; set; }
    }
}