using System;

namespace secNET.Models
{
    public class CCTVLog
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public Branch Branch { get; set; }
        public string OperatorName { get; set; }
        public string ShiftType { get; set; }
        public DateTime Date { get; set; }
        public string ChecklistData { get; set; }
        public double CompliancePercentage { get; set; }
        public string Status { get; set; }
        public string Comments { get; set; }
        public string BranchManagerNotes { get; set; }
        public string ReportSeenBy { get; set; }
        public string BranchManager { get; set; }
        public DateTime? DateOfReportSeen { get; set; }
        public int SubmittedById { get; set; }
        public User SubmittedBy { get; set; }
        public int? ReviewedById { get; set; }
        public User ReviewedBy { get; set; }

        // computed column property
        public DateTime DateOnly { get; private set; } // Private setter since it's computed
    }
}