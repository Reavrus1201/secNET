using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace secNET.Pages.Admin
{
    [Authorize(Policy = "AtLeastTier3")]
    public class IndexModel : PageModel
    {
        private readonly SecNETContext _context;

        public IndexModel(SecNETContext context)
        {
            _context = context;
        }

        public IList<User> Users { get; set; }

        public async Task OnGetAsync()
        {
            Users = await _context.Users.Include(u => u.Branch).ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Log user deletion
                var adminUsername = User.Identity.Name; // Get the current admin's username
                var log = new UserManagementLog
                {
                    ActionType = "Deleted",
                    Username = user.Username,
                    Date = DateTime.Now.Date,
                    Time = DateTime.Now.TimeOfDay,
                    AdminUsername = adminUsername
                };
                _context.UserManagementLogs.Add(log);
                await _context.SaveChangesAsync();

                // Delete the user
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}