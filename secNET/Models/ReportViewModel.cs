using System.Collections.Generic;

namespace secNET.Models
{
    public class ReportViewModel
    {
        public string ReportTitle { get; set; }
        public string Date { get; set; }
        public object Details { get; set; } // Can be LogDetails or IncidentDetails
        public Dictionary<string, ChecklistCategory> ChecklistData { get; set; }
        public string Footnote { get; set; }

        public class LogDetails
        {
            public int Id { get; set; }
            public string SubmittedBy { get; set; }
            public string Branch { get; set; }
            public string Date { get; set; }
            public string OperatorName { get; set; }
            public string ShiftType { get; set; }
            public string Status { get; set; }
            public string CompliancePercentage { get; set; }
            public string Comments { get; set; }
            public string BranchManagerNotes { get; set; }
            public string ReportSeenBy { get; set; }
            public string BranchManager { get; set; }
            public string DateOfReportSeen { get; set; }
            public string ReviewedBy { get; set; }
        }

        public class IncidentDetails
        {
            public int Id { get; set; }
            public string CreatedBy { get; set; }
            public string Branch { get; set; }
            public string IncidentDateTime { get; set; }
            public string Type { get; set; }
            public string FieldsData { get; set; }
        }

        public class ChecklistCategory
        {
            public Dictionary<string, string> Questions { get; set; }
            public Dictionary<string, string> CROComments { get; set; }
            public Dictionary<string, string> ManagersActionTaken { get; set; }
        }
    }
}