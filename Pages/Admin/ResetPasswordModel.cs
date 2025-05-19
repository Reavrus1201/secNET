using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System;
using System.Threading.Tasks;
using Isopoh.Cryptography.Argon2;

namespace secNET.Pages.Admin
{
    public class ResetPasswordModel : PageModel
    {
        private readonly SecNETContext _context;

        public ResetPasswordModel(SecNETContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int Id { get; set; }

        public string Username { get; set; }

        public string NewPassword { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            Id = user.Id;
            Username = user.Username;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
            if (user == null)
            {
                return NotFound();
            }

            // Generate a random temporary password (e.g., 12 characters)
            NewPassword = GenerateRandomPassword(12);

            // Hash the new password using Argon2
            string passwordHash = Argon2.Hash(NewPassword, timeCost: 3, memoryCost: 65536, parallelism: 4);

            // Log the password reset
            var adminUsername = User.Identity.Name; // Get the current admin's username
            var securityLog = new UserSecurityLog
            {
                Username = user.Username,
                ResetDateTime = DateTime.Now,
                AdminUsername = adminUsername,
                NewPassword = NewPassword // Store the plaintext password before hashing
            };
            _context.UserSecurityLogs.Add(securityLog);
            await _context.SaveChangesAsync();

            // Update the user's password hash and hash type
            user.PasswordHash = passwordHash;
            user.HashType = "Argon2";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Set success message
            SuccessMessage = $"Password for {user.Username} has been reset successfully. The new password is: {NewPassword}";

            // Redirect back to the ResetPassword page to display the new password
            return Page();
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            Random random = new Random();
            char[] password = new char[length];
            for (int i = 0; i < length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }
            return new string(password);
        }
    }
}