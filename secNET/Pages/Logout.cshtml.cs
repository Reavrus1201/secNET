using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using secNET.Models; // Added for SecNETContext and UserLoginLog
using Microsoft.EntityFrameworkCore; // Added for database operations
using System; // Added for DateTime
using System.Threading.Tasks; // Added for Task<T>

namespace secNET.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SecNETContext _context; // Added for database access

        public LogoutModel(SecNETContext context) // Added constructor to inject context
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync() // Changed to async to handle database operations
        {
            // Retrieve the login ID from the session
            var loginId = HttpContext.Session.GetInt32("CurrentLoginId");
            if (loginId.HasValue)
            {
                // Find the login log entry
                var loginLog = await _context.UserLoginLogs.FindAsync(loginId.Value);
                if (loginLog != null)
                {
                    // Update the logout time
                    loginLog.LogoutDateTime = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // Clear the login ID from the session
                HttpContext.Session.Remove("CurrentLoginId");
            }

            // Clear the rest of the session
            HttpContext.Session.Clear();

            // Clear the JWT token cookie
            Response.Cookies.Delete("jwtToken");

            // Redirect to the login page
            return RedirectToPage("/Login");
        }
    }
}