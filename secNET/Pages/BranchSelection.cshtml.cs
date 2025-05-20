using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http; // Added for session access

namespace secNET.Pages
{
    public class BranchSelectionModel : PageModel
    {
        private readonly SecNETContext _context;
        private readonly ILogger<BranchSelectionModel> _logger;

        public BranchSelectionModel(SecNETContext context, ILogger<BranchSelectionModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<SelectListItem> Branches { get; set; }

        [BindProperty]
        public int SelectedBranchId { get; set; }

        public async Task OnGetAsync()
        {
            // Populate the Branches property with data from the database
            Branches = await _context.Branches
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.BranchName
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Find the selected branch in the database
            var branch = await _context.Branches.FindAsync(SelectedBranchId);
            if (branch == null)
            {
                _logger.LogWarning($"Branch with ID {SelectedBranchId} not found.");
                return NotFound();
            }

            // Log the selected branch
            _logger.LogInformation($"User selected branch ID: {SelectedBranchId}, Branch Name: {branch.BranchName}");

            // Store the selected branch name and ID in the session
            HttpContext.Session.SetString("SelectedBranch", branch.BranchName);
            HttpContext.Session.SetInt32("SelectedBranchId", branch.Id);

            // Redirect to the dashboard
            return RedirectToPage("/Index");
        }
    }
}