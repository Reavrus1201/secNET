using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace secNET.Pages
{
    [Authorize(Policy = "AtLeastTier2")]
    public class ReportExplorerModel : PageModel
    {
        private readonly SecNETContext _context;
        private readonly ILogger<ReportExplorerModel> _logger;

        public ReportExplorerModel(SecNETContext context, ILogger<ReportExplorerModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public string ReportType { get; set; }

        [BindProperty]
        public string DateRange { get; set; }

        [BindProperty]
        public int? BranchId { get; set; }

        [BindProperty]
        public string Status { get; set; }

        [BindProperty]
        public string Search { get; set; }

        [BindProperty]
        public bool ResetReportType { get; set; } // To handle reset button

        public SelectList ReportTypeOptions { get; set; }
        public SelectList BranchOptions { get; set; }

        public bool IsTier3 { get; set; }
        public int UserBranchId { get; set; }

        public List<CCTVLog> CCTVLogs { get; set; }
        public List<IncidentReport> IncidentReports { get; set; }
        public Dictionary<int, string> BranchNames { get; set; }

        public IActionResult OnGet()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Login");
            }

            IsTier3 = User.IsInRole("Tier3");
            UserBranchId = int.Parse(User.FindFirst("BranchId")?.Value ?? "0");

            ReportTypeOptions = new SelectList(new[]
            {
                new { Value = "CCTVLogs", Text = "CCTV Logs" },
                new { Value = "IncidentReports", Text = "Incident Reports" }
            }, "Value", "Text", ReportType); // Set selected value

            if (IsTier3 && !string.IsNullOrEmpty(ReportType))
            {
                BranchOptions = new SelectList(_context.Branches.Select(b => new { Value = b.Id, Text = b.BranchName }), "Value", "Text");
            }

            _logger.LogInformation($"User {User.Identity.Name} (Tier {(IsTier3 ? 3 : 2)}, BranchId: {UserBranchId}) accessed Report Explorer.");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsTier3 = User.IsInRole("Tier3");
            UserBranchId = int.Parse(User.FindFirst("BranchId")?.Value ?? "0");

            if (ResetReportType)
            {
                ReportType = null; // Reset the report type
                CCTVLogs = null;
                IncidentReports = null;
                BranchId = null;
                DateRange = null;
                Status = null;
                Search = null;
            }

            ReportTypeOptions = new SelectList(new[]
            {
                new { Value = "CCTVLogs", Text = "CCTV Logs" },
                new { Value = "IncidentReports", Text = "Incident Reports" }
            }, "Value", "Text", ReportType); // Persist selected value

            if (IsTier3 && !string.IsNullOrEmpty(ReportType))
            {
                BranchOptions = new SelectList(_context.Branches.Select(b => new { Value = b.Id, Text = b.BranchName }), "Value", "Text");
            }

            if (!string.IsNullOrEmpty(Search)) // Handle Search button click
            {
                if (string.IsNullOrEmpty(ReportType))
                {
                    ModelState.AddModelError("ReportType", "Please select a report type.");
                    return Page();
                }

                _logger.LogInformation($"User {User.Identity.Name} searched: ReportType={ReportType}, DateRange={DateRange}, BranchId={BranchId}, Status={Status}");

                BranchNames = _context.Branches.ToDictionary(b => b.Id, b => b.BranchName);

                if (ReportType == "CCTVLogs")
                {
                    var query = _context.CCTVLogs.AsQueryable();

                    if (IsTier3)
                    {
                        if (BranchId.HasValue && BranchId.Value > 0)
                        {
                            query = query.Where(c => c.BranchId == BranchId.Value);
                        }
                    }
                    else
                    {
                        query = query.Where(c => c.BranchId == UserBranchId);
                    }

                    if (!string.IsNullOrEmpty(DateRange) && DateRange != "All")
                    {
                        var yearMonth = DateRange.Split('-');
                        var year = int.Parse(yearMonth[0]);
                        var month = int.Parse(yearMonth[1]);
                        var startDate = new DateTime(year, month, 1);
                        var endDate = startDate.AddMonths(1).AddDays(-1);
                        query = query.Where(c => c.Date >= startDate && c.Date <= endDate);
                    }

                    if (IsTier3 && !string.IsNullOrEmpty(Status))
                    {
                        query = query.Where(c => c.Status == Status);
                    }

                    CCTVLogs = await query.ToListAsync();
                }
                else if (ReportType == "IncidentReports")
                {
                    var query = _context.IncidentReports.AsQueryable();

                    if (IsTier3)
                    {
                        if (BranchId.HasValue && BranchId.Value > 0)
                        {
                            query = query.Where(i => i.BranchId == BranchId.Value);
                        }
                    }
                    else
                    {
                        query = query.Where(i => i.BranchId == UserBranchId);
                    }

                    if (!string.IsNullOrEmpty(DateRange) && DateRange != "All")
                    {
                        var yearMonth = DateRange.Split('-');
                        var year = int.Parse(yearMonth[0]);
                        var month = int.Parse(yearMonth[1]);
                        var startDate = new DateTime(year, month, 1);
                        var endDate = startDate.AddMonths(1).AddDays(-1);
                        query = query.Where(i => i.IncidentDateTime >= startDate && i.IncidentDateTime <= endDate);
                    }

                    IncidentReports = await query.ToListAsync();
                }
            }

            return Page();
        }
    }
}