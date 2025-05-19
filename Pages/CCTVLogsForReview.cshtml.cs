using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace secNET.Pages
{
    [Authorize(Policy = "AtLeastTier2")]
    public class CCTVLogsForReviewModel : PageModel
    {
        private readonly SecNETContext _context;

        public CCTVLogsForReviewModel(SecNETContext context)
        {
            _context = context;
        }

        public IList<CCTVLog> CCTVLogsForReview { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1; // Default to page 1

        public const int PageSize = 10; // 10 entries per page

        public int TotalPages { get; set; }
        public int TotalItems { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.BranchId == null)
            {
                return Unauthorized();
            }

            bool isSuperUser = user.TierLevel == 3;

            IQueryable<CCTVLog> logsQuery = _context.CCTVLogs
                .Include(l => l.Branch)
                .Where(l => l.Status == "Final" && l.ReviewedById == null);

            if (!isSuperUser)
            {
                logsQuery = logsQuery.Where(l => l.BranchId == user.BranchId);
            }

            TotalItems = await logsQuery.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
            CCTVLogsForReview = await logsQuery
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }
    }
}