using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace secNET.Models
{
    public class CCTVLogViewModel
    {
        public int Id { get; set; }

        [Required]
        public int BranchId { get; set; }
        public string BranchName { get; set; }

        [Required]
        public string OperatorName { get; set; }

        [Required]
        public string ShiftType { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public Dictionary<string, SectionData> Sections { get; set; }

        public double CompliancePercentage { get; set; }

        public string Status { get; set; }

        public string Comments { get; set; }

        public string BranchManagerNotes { get; set; }

        public string ReportSeenBy { get; set; }

        public string BranchManager { get; set; }

        public DateTime? DateOfReportSeen { get; set; }

        public int SubmittedById { get; set; }
        public string SubmittedByUsername { get; set; }

        public int? ReviewedById { get; set; }
        public string ReviewedByUsername { get; set; }

        public int? SelectedBranchId { get; set; }
        public List<Branch> AvailableBranches { get; set; }
    }

    public class SectionData
    {
        public Dictionary<string, string> Questions { get; set; }
        public Dictionary<string, string> CROComments { get; set; }
        public Dictionary<string, string> ManagersActionTaken { get; set; }
    }

    public class ChecklistData
    {
        public Dictionary<string, ChecklistSection> Sections { get; set; }
    }

    public class ChecklistSection
    {
        public Dictionary<string, string> Questions { get; set; }
        public Dictionary<string, string> CROComments { get; set; }
        public Dictionary<string, string> ManagersActionTaken { get; set; }
    }
}