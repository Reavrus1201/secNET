// Login.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System;
using Microsoft.Extensions.Logging;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.Extensions.Configuration; // Add this using directive

namespace secNET.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SecNETContext _context;
        private readonly ILogger<LoginModel> _logger;
        private readonly IConfiguration _configuration; // Add IConfiguration

        public LoginModel(SecNETContext context, ILogger<LoginModel> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration; // Inject IConfiguration
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public bool ShowPasswordStep { get; set; } = false;
        public bool ShowBranchVerification { get; set; } = false;
        public string AssignedBranchName { get; set; }
        public string BranchVerificationMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Username is required.")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("AssignedBranchId");
            HttpContext.Session.Remove("AssignedBranchName");
            return Page();
        }

        public async Task<IActionResult> OnPostUsernameAsync()
        {
            if (string.IsNullOrEmpty(Input.Username))
            {
                ModelState.AddModelError("Input.Username", "Username is required.");
                return Page();
            }

            var userByUsername = await _context.Users
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Username.Trim().ToLower() == Input.Username.Trim().ToLower());

            if (userByUsername == null)
            {
                _logger.LogWarning("User not found by username.");
                ModelState.AddModelError(string.Empty, "Invalid username.");
                return Page();
            }

            ModelState.Clear();
            HttpContext.Session.SetString("Username", Input.Username);
            ShowPasswordStep = true;
            return Page();
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage();
            }

            Input.Username = username;

            if (string.IsNullOrEmpty(Input.Password))
            {
                ModelState.AddModelError("Input.Password", "Password is required.");
                ShowPasswordStep = true;
                return Page();
            }

            var userByUsername = await _context.Users
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Username.Trim().ToLower() == Input.Username.Trim().ToLower());

            if (userByUsername == null)
            {
                _logger.LogWarning("User not found by username during login.");
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                ShowPasswordStep = true;
                return Page();
            }

            bool passwordValid = false;
            if (userByUsername.HashType == "SHA256")
            {
                var sha256Hash = HashPasswordSHA256(Input.Password);
                passwordValid = sha256Hash.Trim().ToLower() == userByUsername.PasswordHash.Trim().ToLower();

                if (passwordValid)
                {
                    userByUsername.PasswordHash = HashPassword(Input.Password);
                    userByUsername.HashType = "Argon2";
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Migrated user {userByUsername.Username} to Argon2 hash.");
                }
            }
            else if (userByUsername.HashType == "Argon2")
            {
                passwordValid = VerifyPassword(Input.Password, userByUsername.PasswordHash);
            }
            else
            {
                _logger.LogError($"Unknown HashType for user {userByUsername.Username}: {userByUsername.HashType}");
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                ShowPasswordStep = true;
                return Page();
            }

            if (!passwordValid)
            {
                _logger.LogWarning("Password verification failed.");
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                ShowPasswordStep = true;
                return Page();
            }

            _logger.LogInformation($"User authenticated: {userByUsername.Username}, TierLevel: {userByUsername.TierLevel}, BranchId: {userByUsername.BranchId}");

            var role = userByUsername.TierLevel switch
            {
                1 => "Tier1",
                2 => "Tier2",
                3 => "Tier3",
                _ => "Tier0"
            };

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userByUsername.Username),
                new Claim(ClaimTypes.Role, role),
                new Claim("TierLevel", userByUsername.TierLevel.ToString()),
                new Claim("BranchId", userByUsername.BranchId.ToString())
            };

            var token = GenerateJwtToken(claims);
            Response.Cookies.Append("jwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddDays(1)
            });

            if (userByUsername.TierLevel == 3)
            {
                HttpContext.Session.SetString("SelectedBranch", "All Branches");
                HttpContext.Session.SetInt32("SelectedBranchId", 0);
                return RedirectToPage("/Index");
            }
            else
            {
                if (userByUsername.BranchId == null || userByUsername.Branch == null)
                {
                    ModelState.AddModelError(string.Empty, "User branch assignment is missing.");
                    ShowPasswordStep = true;
                    return Page();
                }

                AssignedBranchName = userByUsername.Branch.BranchName;
                HttpContext.Session.SetInt32("AssignedBranchId", userByUsername.BranchId.Value);
                HttpContext.Session.SetString("AssignedBranchName", AssignedBranchName);
                ShowBranchVerification = true;
                return Page();
            }
        }

        public IActionResult OnPostVerifyBranchAsync(string action)
        {
            var assignedBranchId = HttpContext.Session.GetInt32("AssignedBranchId");
            var assignedBranchName = HttpContext.Session.GetString("AssignedBranchName");

            if (!assignedBranchId.HasValue || string.IsNullOrEmpty(assignedBranchName))
            {
                return RedirectToPage("/Login");
            }

            if (action == "LogMeIn")
            {
                HttpContext.Session.SetString("SelectedBranch", assignedBranchName);
                HttpContext.Session.SetInt32("SelectedBranchId", assignedBranchId.Value);
                return RedirectToPage("/Index");
            }
            else if (action == "NotMyBranch")
            {
                BranchVerificationMessage = "Please contact your local admin to reassign your Working Branch";
                ShowBranchVerification = true;
                AssignedBranchName = assignedBranchName;
                return Page();
            }

            return RedirectToPage("/Login");
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

        private bool VerifyPassword(string password, string storedHash)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            return Argon2.Verify(storedHash, passwordBytes);
        }

        private string HashPasswordSHA256(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private string GenerateJwtToken(Claim[] claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("JwtSettings:SecretKey").Value));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken("secNET", "secNET", claims, expires: DateTime.Now.AddDays(1), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}