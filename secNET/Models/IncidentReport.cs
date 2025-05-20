using System;

namespace secNET.Models
{
    public class IncidentReport
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int BranchId { get; set; }
        public Branch Branch { get; set; }
        public DateTime IncidentDateTime { get; set; }
        public string FieldsData { get; set; }
        public int CreatedById { get; set; }
        public User CreatedBy { get; set; }
    }
}