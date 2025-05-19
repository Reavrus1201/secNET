namespace secNET.Models
{
    public class ChecklistViolation
    {
        public int Id { get; set; }
        public int CCTVLogId { get; set; }
        public string Question { get; set; }
        public string Section { get; set; }
    }
}