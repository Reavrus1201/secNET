using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace secNET.Pages
{
    [Authorize(Policy = "AtLeastTier1")]
    public class IncidentReportModel : PageModel
    {
        private readonly SecNETContext _context;

        public IncidentReportModel(SecNETContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IncidentReportViewModel IncidentReport { get; set; }

        public SelectList Branches { get; set; }
        public SelectList ReportTypes { get; set; }

        public async Task OnGetAsync()
        {
            // Get the current user
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.BranchId == null)
            {
                throw new InvalidOperationException("User or BranchId not found.");
            }

            // Check if the user is a super user (Tier 3)
            bool isSuperUser = user.TierLevel == 3;
            ViewData["IsSuperUser"] = isSuperUser;
            ViewData["UserBranchId"] = user.BranchId;

            // Populate branches dropdown
            Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "BranchName", user.BranchId);
            ReportTypes = new SelectList(new List<string> { "Policy Violation", "Criminal", "General" });

            IncidentReport = new IncidentReportViewModel
            {
                Type = "Policy Violation",
                BranchId = user.BranchId.Value, // Prefill with user's branch
                IncidentDate = DateTime.Today,
                IncidentTime = "12:00 AM",
                SuspectType = "Adult"
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Get the current user
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.BranchId == null)
            {
                return Unauthorized();
            }

            // Check if the user is a super user (Tier 3)
            bool isSuperUser = user.TierLevel == 3;
            ViewData["IsSuperUser"] = isSuperUser;
            ViewData["UserBranchId"] = user.BranchId;

            if (!ModelState.IsValid)
            {
                Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "BranchName", user.BranchId);
                ReportTypes = new SelectList(new List<string> { "Policy Violation", "Criminal", "General" });
                return Page();
            }

            // For Tier 1 and Tier 2 users, enforce the BranchId from login
            if (!isSuperUser)
            {
                IncidentReport.BranchId = user.BranchId.Value;
            }

            // Additional validation for Criminal reports
            if (IncidentReport.Type == "Criminal")
            {
                if (IncidentReport.SuspectType == "Minor")
                {
                    if (IncidentReport.Age == null || string.IsNullOrWhiteSpace(IncidentReport.ContactDetails))
                    {
                        ModelState.AddModelError("", "Age and Contact Details are required for minors.");
                        Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "BranchName", user.BranchId);
                        ReportTypes = new SelectList(new List<string> { "Policy Violation", "Criminal", "General" });
                        return Page();
                    }
                }
                if (IncidentReport.ValueItems == null || IncidentReport.PenaltyPaid == null)
                {
                    ModelState.AddModelError("", "Value of Items and Penalty Paid must be provided for criminal reports.");
                    Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "BranchName", user.BranchId);
                    ReportTypes = new SelectList(new List<string> { "Policy Violation", "Criminal", "General" });
                    return Page();
                }
            }

            // Get branch name
            var branch = await _context.Branches.FindAsync(IncidentReport.BranchId);
            IncidentReport.BranchName = branch?.BranchName;

            // Prepare data for HTML and database
            var fieldsData = new Dictionary<string, object>
            {
                { "ReportedBy", IncidentReport.ReportedBy },
                { "Description", IncidentReport.Description },
                { "ActionTaken", IncidentReport.ActionTaken },
                { "SeenBy", IncidentReport.SeenBy },
                { "AdditionalNotes", IncidentReport.AdditionalNotes ?? "" }
            };

            if (IncidentReport.Type == "Criminal")
            {
                fieldsData.Add("SuspectType", IncidentReport.SuspectType);
                fieldsData.Add("SuspectName", IncidentReport.SuspectName ?? "");
                fieldsData.Add("ListItems", IncidentReport.ListItems ?? "");
                fieldsData.Add("ValueItems", IncidentReport.ValueItems);
                fieldsData.Add("PenaltyPaid", IncidentReport.PenaltyPaid);
                fieldsData.Add("CrOb", IncidentReport.CrOb ?? "");
                if (IncidentReport.SuspectType == "Minor")
                {
                    fieldsData.Add("Age", IncidentReport.Age);
                    fieldsData.Add("ContactDetails", IncidentReport.ContactDetails ?? "");
                }
            }

            // Combine date and time for IncidentDateTime
            if (!TimeSpan.TryParse(IncidentReport.IncidentTime, out var timeSpan))
            {
                ModelState.AddModelError("IncidentReport.IncidentTime", "Invalid time format.");
                Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "BranchName", user.BranchId);
                ReportTypes = new SelectList(new List<string> { "Policy Violation", "Criminal", "General" });
                return Page();
            }
            var incidentDateTime = IncidentReport.IncidentDate.Date.Add(timeSpan);

            // Generate HTML report
            var html = GenerateHtmlReport(IncidentReport, fieldsData);
            var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Incident Reports");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"Incident_Report_{IncidentReport.Type.Replace(" ", "_")}_{timestamp}.html";
            var outputPath = Path.Combine(outputDir, filename);
            await System.IO.File.WriteAllTextAsync(outputPath, html);

            // Log to database
            var incidentReport = new IncidentReport
            {
                Type = IncidentReport.Type,
                BranchId = IncidentReport.BranchId,
                IncidentDateTime = incidentDateTime,
                FieldsData = JsonSerializer.Serialize(fieldsData),
                CreatedById = user.Id
            };
            _context.IncidentReports.Add(incidentReport);
            await _context.SaveChangesAsync();

            // Set success message with path
            TempData["SuccessMessage"] = $"Report created successfully at {outputPath}";

            return RedirectToPage("/Index");
        }

        private string GenerateHtmlReport(IncidentReportViewModel model, Dictionary<string, object> fieldsData)
        {
            // Define colors based on report type
            string headerColor, headerBgColor, borderColor;
            switch (model.Type)
            {
                case "Policy Violation":
                    headerColor = "#2E8BFF"; // Blue
                    headerBgColor = "#E6F0FF"; // Light blue
                    borderColor = "#2E8BFF";
                    break;
                case "Criminal":
                    headerColor = "#FF4500"; // Red
                    headerBgColor = "#FFE6E6"; // Light red
                    borderColor = "#FF4500";
                    break;
                case "General":
                    headerColor = "#32CD32"; // Green
                    headerBgColor = "#E6FFE6"; // Light green
                    borderColor = "#32CD32";
                    break;
                default:
                    headerColor = "#000000";
                    headerBgColor = "#FFFFFF";
                    borderColor = "#000000";
                    break;
            }

            // Base HTML template
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"UTF-8\">");
            html.AppendLine($"<title>Incident Report - {model.Type}</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; font-size: 12pt; color: #000000; background-color: #ffffff; margin: 20mm; }");
            html.AppendLine(".header { text-align: center; margin-bottom: 20px; border-bottom: 2px solid " + borderColor + "; padding-bottom: 10px; }");
            html.AppendLine(".header h1 { font-size: 24pt; color: " + headerColor + "; margin: 0; font-weight: bold; }");
            html.AppendLine(".header h2 { font-size: 18pt; color: " + headerColor + "; margin: 5px 0 0 0; }");
            html.AppendLine(".report-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
            html.AppendLine(".report-table th, .report-table td { border: 1px solid #CCCCCC; padding: 8px; text-align: left; }");
            html.AppendLine(".report-table th { background-color: " + headerBgColor + "; font-weight: bold; }");
            html.AppendLine(".report-table td { background-color: #FFFFFF; }");
            html.AppendLine(".footnote { display: flex; align-items: center; flex-wrap: wrap; margin-top: 20px; font-size: 10pt; color: #333333; }");
            html.AppendLine(".footnote .text-content { display: flex; flex-direction: column; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Header
            html.AppendLine("<div class=\"header\">");
            html.AppendLine("<h1>Woermann Brock Security</h1>");
            html.AppendLine($"<h2>{model.Type} Report</h2>");
            html.AppendLine("</div>");

            // Table
            html.AppendLine("<table class=\"report-table\">");
            html.AppendLine("<tr><th>Field</th><th>Value</th></tr>");
            html.AppendLine($"<tr><td>Incident Date</td><td>{model.IncidentDate:yyyy-MM-dd}</td></tr>");
            html.AppendLine($"<tr><td>Incident Time</td><td>{model.IncidentTime}</td></tr>");
            html.AppendLine($"<tr><td>Store Name</td><td>{model.BranchName}</td></tr>");
            html.AppendLine($"<tr><td>Reported By</td><td>{model.ReportedBy}</td></tr>");
            html.AppendLine($"<tr><td>Incident Description</td><td>{model.Description}</td></tr>");
            html.AppendLine($"<tr><td>Action Taken</td><td>{model.ActionTaken}</td></tr>");
            html.AppendLine($"<tr><td>Incident Reported or Seen By</td><td>{model.SeenBy}</td></tr>");
            html.AppendLine($"<tr><td>Additional Notes</td><td>{model.AdditionalNotes ?? ""}</td></tr>");

            // Criminal-specific fields
            if (model.Type == "Criminal")
            {
                html.AppendLine($"<tr><td>Suspect Type</td><td>{model.SuspectType}</td></tr>");
                html.AppendLine($"<tr><td>Name/Surname/ID of Suspect</td><td>{model.SuspectName ?? ""}</td></tr>");
                html.AppendLine($"<tr><td>List Items</td><td>{model.ListItems ?? ""}</td></tr>");
                html.AppendLine($"<tr><td>Value of Items</td><td>{model.ValueItems}</td></tr>");
                html.AppendLine($"<tr><td>Penalty Paid</td><td>{model.PenaltyPaid}</td></tr>");
                html.AppendLine($"<tr><td>CR#/OB#</td><td>{model.CrOb ?? ""}</td></tr>");
                if (model.SuspectType == "Minor")
                {
                    html.AppendLine($"<tr><td>Age</td><td>{model.Age}</td></tr>");
                    html.AppendLine($"<tr><td>Guardian/Parent/Teacher Contact</td><td>{model.ContactDetails ?? ""}</td></tr>");
                }
            }

            html.AppendLine("</table>");

            // Footnote (without logo)
            html.AppendLine("<div class=\"footnote\">");
            html.AppendLine("<div class=\"text-content\">");
            html.AppendLine("<span>Created by secNET</span>");
            html.AppendLine($"<span>Page <span class=\"page\">1</span> of <span class=\"topage\">1</span> | Generated: {model.IncidentDate:yyyy-MM-dd}</span>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // Simplified pagination script (single page)
            html.AppendLine("<script type=\"text/javascript\">");
            html.AppendLine("window.onload = function() {");
            html.AppendLine("var pages = document.getElementsByClassName('page');");
            html.AppendLine("var topages = document.getElementsByClassName('topage');");
            html.AppendLine("for (var i = 0; i < pages.length; i++) {");
            html.AppendLine("pages[i].innerHTML = 1;");
            html.AppendLine("topages[i].innerHTML = 1;");
            html.AppendLine("}");
            html.AppendLine("};");
            html.AppendLine("</script>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }
    }
}