using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace secNET.Pages
{
    public class IndexModel : PageModel
    {
        private readonly SecNETContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(SecNETContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Summary card data
        public double AvgCompliance { get; set; }
        public int TotalIncidents { get; set; }

        // Chart data
        public Dictionary<string, double> AvgComplianceByBranch { get; set; }
        public Dictionary<string, int> IncidentsByType { get; set; }
        public List<(string Question, int NoCount)> TopPolicyViolations { get; set; }
        public Dictionary<string, double> IncidentSeverityByBranch { get; set; }
        public List<(DateTime Date, int Count)> SubmissionFrequency { get; set; }
        public List<(DateTime Month, double Compliance)> ComplianceTrend { get; set; }

        // Serialized data for Chart.js
        public string ComplianceChartLabels { get; set; }
        public string ComplianceChartData { get; set; }
        public string IncidentsChartLabels { get; set; }
        public string IncidentsChartData { get; set; }
        public string SeverityChartLabels { get; set; }
        public string SeverityChartData { get; set; }
        public string SubmissionChartLabels { get; set; }
        public string SubmissionChartData { get; set; }
        public string TrendChartLabels { get; set; }
        public string TrendChartData { get; set; }

        // Filter properties (for Tier 3)
        [BindProperty]
        public List<int> SelectedBranchIds { get; set; }
        [BindProperty]
        public DateTime? FromDate { get; set; }
        [BindProperty]
        public DateTime? ToDate { get; set; }
        [BindProperty]
        public string Month1 { get; set; }
        [BindProperty]
        public string Month2 { get; set; }

        public bool IsTier3 { get; set; }
        public int UserBranchId { get; set; }
        public List<SelectListItem> Branches { get; set; }
        public List<SelectListItem> Months { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Login");

            IsTier3 = User.IsInRole("Tier3");
            UserBranchId = int.Parse(User.FindFirst("BranchId")?.Value ?? "0");

            // Set default filter values
            var now = DateTime.Now;
            FromDate = now.AddDays(-30);
            ToDate = now;
            SelectedBranchIds = IsTier3 ? new List<int>() : new List<int> { UserBranchId };
            Month1 = now.AddMonths(-1).ToString("yyyy-MM");
            Month2 = now.ToString("yyyy-MM");

            // Populate filter options
            Branches = await _context.Branches
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.BranchName })
                .ToListAsync();
            Months = Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-i))
                .Select(m => new SelectListItem { Value = m.ToString("yyyy-MM"), Text = m.ToString("MMMM yyyy") })
                .ToList();

            await LoadDashboardData();
            return Page();
        }

        public async Task<IActionResult> OnPostApplyFiltersAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Login");

            IsTier3 = User.IsInRole("Tier3");
            UserBranchId = int.Parse(User.FindFirst("BranchId")?.Value ?? "0");

            // Populate filter options
            Branches = await _context.Branches
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.BranchName })
                .ToListAsync();
            var now = DateTime.Now;
            Months = Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-i))
                .Select(m => new SelectListItem { Value = m.ToString("yyyy-MM"), Text = m.ToString("MMMM yyyy") })
                .ToList();

            if (!IsTier3)
            {
                SelectedBranchIds = new List<int> { UserBranchId };
            }

            await LoadDashboardData();
            return Page();
        }

        public IActionResult OnPostResetFilters()
        {
            return RedirectToPage();
        }

        private async Task LoadDashboardData()
        {
            var from = FromDate ?? DateTime.Now.AddDays(-30);
            var to = ToDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;
            var branchIds = SelectedBranchIds.Any() ? SelectedBranchIds : (IsTier3 ? await _context.Branches.Select(b => b.Id).ToListAsync() : new List<int> { UserBranchId });

            // Summary Cards
            AvgCompliance = await _context.CCTVLogs
                .Where(l => branchIds.Contains(l.BranchId) && l.Date >= from && l.Date <= to)
                .AverageAsync(l => (double?)l.CompliancePercentage) ?? 0.0;
            TotalIncidents = await _context.IncidentReports
                .Where(i => branchIds.Contains(i.BranchId) && i.IncidentDateTime >= from && i.IncidentDateTime <= to)
                .CountAsync();

            // Metric 1: Average Compliance Rate by Branch
            AvgComplianceByBranch = await _context.CCTVLogs
                .Where(l => branchIds.Contains(l.BranchId) && l.Date >= from && l.Date <= to)
                .GroupBy(l => l.Branch.BranchName)
                .Select(g => new { BranchName = g.Key, AvgCompliance = g.Average(l => l.CompliancePercentage) })
                .ToDictionaryAsync(g => g.BranchName, g => g.AvgCompliance);

            ComplianceChartLabels = JsonSerializer.Serialize(AvgComplianceByBranch.Keys);
            ComplianceChartData = JsonSerializer.Serialize(AvgComplianceByBranch.Values);

            // Metric 2: Number of Incidents by Type
            IncidentsByType = await _context.IncidentReports
                .Where(i => branchIds.Contains(i.BranchId) && i.IncidentDateTime >= from && i.IncidentDateTime <= to)
                .GroupBy(i => i.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Type ?? "Unknown", g => g.Count); // Handle null types if any

            IncidentsChartLabels = JsonSerializer.Serialize(IncidentsByType.Keys);
            IncidentsChartData = JsonSerializer.Serialize(IncidentsByType.Values);

            // Metric 3: Top Policy Violations
            var violationsQuery = await _context.ChecklistViolations
                .Join(_context.CCTVLogs,
                      cv => cv.CCTVLogId,
                      log => log.Id,
                      (cv, log) => new { cv, log })
                .Where(joined => branchIds.Contains(joined.log.BranchId) && joined.log.Date >= from && joined.log.Date <= to)
                .GroupBy(joined => new { joined.cv.Question, joined.cv.Section })
                .Select(g => new { Question = g.Key.Question, Section = g.Key.Section, NoCount = g.Count() })
                .OrderByDescending(x => x.NoCount)
                .Take(5)
                .ToListAsync();

            TopPolicyViolations = violationsQuery.Select(x => (Question: $"{x.Section} - {x.Question}", NoCount: x.NoCount)).ToList();

            // Metric 4: Incident Severity Index
            var incidents = await _context.IncidentReports
                .Where(i => branchIds.Contains(i.BranchId) && i.IncidentDateTime >= from && i.IncidentDateTime <= to)
                .Select(i => new { i.BranchId, i.IncidentDateTime, i.Type, BranchName = i.Branch.BranchName })
                .ToListAsync();
            IncidentSeverityByBranch = incidents
                .GroupBy(i => i.BranchName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(i => i.Type switch { "Criminal" => 3.0, "Policy Violation" => 2.0, _ => 1.0 }));

            SeverityChartLabels = JsonSerializer.Serialize(IncidentSeverityByBranch.Keys);
            SeverityChartData = JsonSerializer.Serialize(IncidentSeverityByBranch.Values);

            // Metric 5: CCTV Log Submission Frequency
            var submissionData = await _context.CCTVLogs
                .Where(l => branchIds.Contains(l.BranchId) && l.Date >= from && l.Date <= to)
                .GroupBy(l => l.DateOnly)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();
            SubmissionFrequency = submissionData.Select(g => (g.Date, g.Count)).ToList();

            SubmissionChartLabels = JsonSerializer.Serialize(SubmissionFrequency.Select(x => x.Date.ToString("yyyy-MM-dd")));
            SubmissionChartData = JsonSerializer.Serialize(SubmissionFrequency.Select(x => x.Count));

            // Metric 6: Branch Compliance Trend
            if (!string.IsNullOrEmpty(Month1) && !string.IsNullOrEmpty(Month2))
            {
                var month1Start = DateTime.Parse($"{Month1}-01");
                var month1End = month1Start.AddMonths(1).AddDays(-1);
                var month2Start = DateTime.Parse($"{Month2}-01");
                var month2End = month2Start.AddMonths(1).AddDays(-1);
                var trendData = new List<(DateTime Month, double Compliance)>();
                var month1Compliance = await _context.CCTVLogs
                    .Where(l => branchIds.Contains(l.BranchId) && l.Date >= month1Start && l.Date <= month1End)
                    .AverageAsync(l => (double?)l.CompliancePercentage) ?? 0.0;
                var month2Compliance = await _context.CCTVLogs
                    .Where(l => branchIds.Contains(l.BranchId) && l.Date >= month2Start && l.Date <= month2End)
                    .AverageAsync(l => (double?)l.CompliancePercentage) ?? 0.0;
                trendData.Add((month1Start, month1Compliance));
                trendData.Add((month2Start, month2Compliance));
                ComplianceTrend = trendData;
            }
            else
            {
                var trendData = await _context.CCTVLogs
                    .Where(l => branchIds.Contains(l.BranchId) && l.Date >= from && l.Date <= to)
                    .GroupBy(l => new { l.Date.Year, l.Date.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Compliance = g.Average(l => l.CompliancePercentage)
                    })
                    .OrderBy(g => g.Year).ThenBy(g => g.Month)
                    .ToListAsync();
                ComplianceTrend = trendData.Select(g => (new DateTime(g.Year, g.Month, 1), g.Compliance)).ToList();
            }

            TrendChartLabels = JsonSerializer.Serialize(ComplianceTrend.Select(x => x.Month.ToString("yyyy-MM")));
            TrendChartData = JsonSerializer.Serialize(ComplianceTrend.Select(x => x.Compliance));
        }
    }

    public static class Extensions
    {
        public static Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(
            this IQueryable<KeyValuePair<TKey, TValue>> query)
        {
            return query.ToListAsync().ContinueWith(t => t.Result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
    }
}