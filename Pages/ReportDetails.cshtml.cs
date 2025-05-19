using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using secNET.Models; // Ensure this is present for ReportViewModel
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using System;

namespace secNET.Pages
{
    [Authorize(Policy = "AtLeastTier2")]
    public class ReportDetailsModel : PageModel
    {
        private readonly SecNETContext _context;
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public ReportDetailsModel(SecNETContext context, IRazorViewEngine razorViewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
        {
            _context = context;
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public CCTVLog CCTVLog { get; set; }
        public IncidentReport IncidentReport { get; set; }
        public string ReportType { get; set; }
        public string BranchName { get; set; }
        public bool IsTier3 { get; set; }
        public int UserBranchId { get; set; }
        public Dictionary<string, ReportViewModel.ChecklistCategory> ChecklistData { get; set; } // Updated to use ReportViewModel.ChecklistCategory

        public async Task<IActionResult> OnGetAsync(string reportType, int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Login");
            }

            IsTier3 = User.IsInRole("Tier3");
            UserBranchId = int.Parse(User.FindFirst("BranchId")?.Value ?? "0");

            ReportType = reportType;

            if (string.IsNullOrEmpty(ReportType) || (ReportType != "CCTVLogs" && ReportType != "IncidentReports"))
            {
                return NotFound("Invalid report type.");
            }

            if (ReportType == "CCTVLogs")
            {
                CCTVLog = await _context.CCTVLogs
                    .Include(c => c.SubmittedBy)
                    .Include(c => c.ReviewedBy)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (CCTVLog == null)
                {
                    return NotFound("CCTV Log not found.");
                }

                if (!IsTier3 && CCTVLog.BranchId != UserBranchId)
                {
                    return Forbid();
                }

                var branch = await _context.Branches.FindAsync(CCTVLog.BranchId);
                BranchName = branch?.BranchName ?? "Unknown";

                if (!string.IsNullOrEmpty(CCTVLog.ChecklistData))
                {
                    ChecklistData = JsonSerializer.Deserialize<Dictionary<string, ReportViewModel.ChecklistCategory>>(CCTVLog.ChecklistData);
                }
            }
            else if (ReportType == "IncidentReports")
            {
                IncidentReport = await _context.IncidentReports
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (IncidentReport == null)
                {
                    return NotFound("Incident Report not found.");
                }

                if (!IsTier3 && IncidentReport.BranchId != UserBranchId)
                {
                    return Forbid();
                }

                var branch = await _context.Branches.FindAsync(IncidentReport.BranchId);
                BranchName = branch?.BranchName ?? "Unknown";
            }

            return Page();
        }

        public IActionResult OnGetDownloadHtml(string reportType, int id)
        {
            if (string.IsNullOrEmpty(reportType) || (reportType != "CCTVLogs" && reportType != "IncidentReports"))
            {
                return NotFound("Invalid report type.");
            }

            if (reportType == "CCTVLogs")
            {
                var cctvLog = _context.CCTVLogs
                    .Include(c => c.SubmittedBy)
                    .Include(c => c.ReviewedBy)
                    .FirstOrDefault(c => c.Id == id);
                if (cctvLog == null) return NotFound("CCTV Log not found.");

                var branch = _context.Branches.Find(cctvLog.BranchId);
                var branchName = branch?.BranchName ?? "Unknown";
                var checklistData = JsonSerializer.Deserialize<Dictionary<string, ReportViewModel.ChecklistCategory>>(cctvLog.ChecklistData ?? "{}");
                var submittedBy = cctvLog.SubmittedBy?.Username ?? cctvLog.SubmittedById.ToString();
                var reviewedBy = cctvLog.ReviewedBy?.Username ?? (cctvLog.ReviewedById?.ToString() ?? "Not reviewed");

                var viewModel = new ReportViewModel
                {
                    ReportTitle = "CCTV Log Report",
                    Date = DateTime.Now.ToString("yyyy-MM-dd"),
                    Details = new ReportViewModel.LogDetails
                    {
                        Id = cctvLog.Id,
                        SubmittedBy = submittedBy,
                        Branch = branchName,
                        Date = cctvLog.Date.ToString("yyyy-MM-dd"),
                        OperatorName = cctvLog.OperatorName,
                        ShiftType = cctvLog.ShiftType,
                        Status = cctvLog.Status,
                        CompliancePercentage = $"{cctvLog.CompliancePercentage}%",
                        Comments = cctvLog.Comments,
                        BranchManagerNotes = cctvLog.BranchManagerNotes,
                        ReportSeenBy = cctvLog.ReportSeenBy,
                        BranchManager = cctvLog.BranchManager,
                        DateOfReportSeen = cctvLog.DateOfReportSeen?.ToString("yyyy-MM-dd") ?? "Not seen",
                        ReviewedBy = reviewedBy
                    },
                    ChecklistData = checklistData,
                    Footnote = "Report created and printed by secNET for Woermann Brock & Co. Pty. Ltd. All rights reserved."
                };

                var htmlContent = RenderViewToString("ReportDownload", viewModel);
                var fileName = $"CCTV_Log_{cctvLog.Id}_{DateTime.Now:yyyyMMddHHmmss}.html";
                var bytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);
                return File(bytes, "text/html", fileName);
            }
            else if (reportType == "IncidentReports")
            {
                var incidentReport = _context.IncidentReports.FirstOrDefault(i => i.Id == id);
                if (incidentReport == null) return NotFound("Incident Report not found.");

                var branch = _context.Branches.Find(incidentReport.BranchId);
                var branchName = branch?.BranchName ?? "Unknown";
                var createdBy = _context.Users.FirstOrDefault(u => u.Id == incidentReport.CreatedById)?.Username ?? incidentReport.CreatedById.ToString();

                var viewModel = new ReportViewModel
                {
                    ReportTitle = "Incident Report",
                    Date = DateTime.Now.ToString("yyyy-MM-dd"),
                    Details = new ReportViewModel.IncidentDetails
                    {
                        Id = incidentReport.Id,
                        CreatedBy = createdBy,
                        Branch = branchName,
                        IncidentDateTime = incidentReport.IncidentDateTime.ToString("yyyy-MM-dd HH:mm"),
                        Type = incidentReport.Type,
                        FieldsData = incidentReport.FieldsData
                    },
                    Footnote = "Report created and printed by secNET for Woermann Brock & Co. Pty. Ltd. All rights reserved."
                };

                var htmlContent = RenderViewToString("ReportDownload", viewModel);
                var fileName = $"Incident_Report_{incidentReport.Id}_{DateTime.Now:yyyyMMddHHmmss}.html";
                var bytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);
                return File(bytes, "text/html", fileName);
            }

            return NotFound();
        }

        private string RenderViewToString(string viewName, object model)
        {
            var actionContext = new ActionContext(HttpContext, RouteData, new PageActionDescriptor());
            var tempData = new TempDataDictionary(HttpContext, _tempDataProvider);

            using (var writer = new StringWriter())
            {
                var viewResult = _razorViewEngine.GetView("~/Pages", $"/Pages/{viewName}.cshtml", false);
                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"View {viewName} not found.");
                }

                var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                };

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewData,
                    tempData,
                    writer,
                    new HtmlHelperOptions()
                );

                viewResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
                return writer.GetStringBuilder().ToString();
            }
        }
    }
}