using System;
using System.ComponentModel.DataAnnotations;

namespace secNET.Models
{
    public class IncidentReportViewModel
    {
        [Required(ErrorMessage = "Report Type is required.")]
        public string Type { get; set; }

        [Required(ErrorMessage = "Branch is required.")]
        public int BranchId { get; set; }

        public string BranchName { get; set; }

        [Required(ErrorMessage = "Incident Date is required.")]
        [DataType(DataType.Date)]
        public DateTime IncidentDate { get; set; }

        [Required(ErrorMessage = "Incident Time is required.")]
        public string IncidentTime { get; set; }

        [Required(ErrorMessage = "Reported By is required.")]
        public string ReportedBy { get; set; }

        [Required(ErrorMessage = "Incident Description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Action Taken is required.")]
        public string ActionTaken { get; set; }

        [Required(ErrorMessage = "Incident Reported or Seen By is required.")]
        public string SeenBy { get; set; }

        public string AdditionalNotes { get; set; }

        // Criminal-specific fields
        public string SuspectType { get; set; }
        public string SuspectName { get; set; }
        public string ListItems { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Value of Items must be a positive number.")]
        public decimal? ValueItems { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Penalty Paid must be a positive number.")]
        public decimal? PenaltyPaid { get; set; }

        public string CrOb { get; set; }

        [Range(0, 17, ErrorMessage = "Age must be between 0 and 17 for minors.")]
        public int? Age { get; set; }

        public string ContactDetails { get; set; }

        // New fields for General and Policy Violation report types
        public string GeneralDetails { get; set; }
        public string PolicyDetails { get; set; }
    }
}