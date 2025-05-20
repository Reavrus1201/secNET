using System.ComponentModel.DataAnnotations;

namespace secNET.Models
{
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, ErrorMessage = "Username cannot be longer than 50 characters.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Tier Level is required.")]
        [Range(1, 3, ErrorMessage = "Tier Level must be between 1 and 3.")]
        public int TierLevel { get; set; }

        public int? BranchId { get; set; }
    }
}