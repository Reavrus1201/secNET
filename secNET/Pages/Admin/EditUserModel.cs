using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System.Linq;
using System.Threading.Tasks;
using System; // Added for DateTime

namespace secNET.Pages.Admin
{
    public class EditUserModel : PageModel
    {
        private readonly SecNETContext _context;

        public EditUserModel(SecNETContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EditUserViewModel Input { get; set; }

        public SelectList TierLevels { get; set; }
        public SelectList Branches { get; set; }

        [TempData]
        public string SuccessMessage { get; set; } // Add TempData property for success message

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _context.Users.Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            Input = new EditUserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                TierLevel = user.TierLevel,
                BranchId = user.BranchId
            };

            TierLevels = new SelectList(Enumerable.Range(1, 3).Select(i => new { Id = i, Name = $"Tier {i}" }), "Id", "Name", Input.TierLevel);
            Branches = new SelectList(_context.Branches, "Id", "BranchName", Input.BranchId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                TierLevels = new SelectList(Enumerable.Range(1, 3).Select(i => new { Id = i, Name = $"Tier {i}" }), "Id", "Name", Input.TierLevel);
                Branches = new SelectList(_context.Branches, "Id", "BranchName", Input.BranchId);
                return Page();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Log the edit action
            var adminUsername = User.Identity.Name;
            var log = new UserManagementLog
            {
                ActionType = "Edited",
                Username = user.Username,
                Date = DateTime.Now.Date,
                Time = DateTime.Now.TimeOfDay,
                AdminUsername = adminUsername
            };
            _context.UserManagementLogs.Add(log);
            await _context.SaveChangesAsync();

            // Update the user
            user.Username = Input.Username;
            user.TierLevel = Input.TierLevel;
            user.BranchId = Input.BranchId == 0 ? null : Input.BranchId;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Set success message in TempData
            SuccessMessage = "User updated successfully!";

            return RedirectToPage("./Index");
        }
    }
}