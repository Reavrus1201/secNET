using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Isopoh.Cryptography.Argon2;
using System;

namespace secNET.Pages.Admin
{
    [Authorize(Policy = "AtLeastTier3")]
    public class AddUserModel : PageModel
    {
        private readonly SecNETContext _context;

        public AddUserModel(SecNETContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Username is required.")]
            [StringLength(50, ErrorMessage = "Username must be between 1 and 50 characters.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Tier level is required.")]
            [Range(1, 3, ErrorMessage = "Tier level must be 1, 2, or 3.")]
            public int TierLevel { get; set; }

            [Required(ErrorMessage = "Branch is required.")]
            public int BranchId { get; set; }
        }

        public SelectList TierLevels { get; set; }
        public SelectList Branches { get; set; }

        public async Task OnGetAsync()
        {
            // Populate tier levels dropdown (1 = CCTV Operator, 2 = Branch Manager/Supervisor, 3 = Risk Officer/GM)
            TierLevels = new SelectList(new[]
            {
                new { Id = 1, Name = "Tier 1 - CCTV Operator" },
                new { Id = 2, Name = "Tier 2 - Branch Manager/Supervisor" },
                new { Id = 3, Name = "Tier 3 - Risk Officer/GM" }
            }, "Id", "Name");

            // Populate branches dropdown
            Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "BranchName");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Repopulate dropdowns
                return Page();
            }

            // Check if username already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.Trim().ToLower() == Input.Username.Trim().ToLower());
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Username", "Username already exists.");
                await OnGetAsync(); // Repopulate dropdowns
                return Page();
            }

            // Create new user
            var user = new User
            {
                Username = Input.Username,
                PasswordHash = HashPassword(Input.Password),
                HashType = "Argon2",
                TierLevel = Input.TierLevel,
                BranchId = Input.BranchId
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Log new user creation
            var adminUsername = User.Identity.Name; // Get the current admin's username
            var log = new UserManagementLog
            {
                ActionType = "New",
                Username = user.Username,
                Date = DateTime.Now.Date,
                Time = DateTime.Now.TimeOfDay,
                AdminUsername = adminUsername
            };
            _context.UserManagementLogs.Add(log);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private string HashPassword(string password)
        {
            var salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            var config = new Argon2Config
            {
                Type = Argon2Type.HybridAddressing,
                TimeCost = 3,
                MemoryCost = 65536,
                Threads = 4,
                Password = Encoding.UTF8.GetBytes(password),
                Salt = salt,
                HashLength = 32
            };

            using (var argon2 = new Argon2(config))
            {
                var hashArray = argon2.Hash();
                return config.EncodeString(hashArray.Buffer);
            }
        }
    }
}