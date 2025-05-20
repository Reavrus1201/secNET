using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using secNET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace secNET.Pages
{
    [Authorize(Policy = "AtLeastTier2")]
    public class ReviewCCTVLogModel : PageModel
    {
        private readonly SecNETContext _context;
        private readonly ILogger<ReviewCCTVLogModel> _logger;

        public ReviewCCTVLogModel(SecNETContext context, ILogger<ReviewCCTVLogModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public CCTVLogViewModel CCTVLog { get; set; }

        [BindProperty]
        public string[] SectionKeys { get; set; }

        [BindProperty]
        public string[][] QuestionKeys { get; set; }

        [BindProperty]
        public string[][] QuestionValues { get; set; }

        [BindProperty]
        public string[][] CROComments { get; set; }

        [BindProperty]
        public string[][] ManagersActionTaken { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.BranchId == null)
            {
                return Unauthorized();
            }

            bool isSuperUser = user.TierLevel == 3;
            ViewData["IsSuperUser"] = isSuperUser;

            IQueryable<CCTVLog> logQuery = _context.CCTVLogs
                .Where(l => l.Id == id && l.Status == "Final" && l.ReviewedById == null);

            if (!isSuperUser)
            {
                logQuery = logQuery.Where(l => l.BranchId == user.BranchId);
            }

            var log = await logQuery.FirstOrDefaultAsync();

            if (log == null)
            {
                _logger.LogWarning($"Log not found in OnGetAsync. LogId: {id}, User: {username}, BranchId: {user.BranchId}, IsSuperUser: {isSuperUser}");
                return NotFound();
            }

            float calculatedCompliance = CalculateCompliancePercentage(log.ChecklistData);
            if (Math.Abs(log.CompliancePercentage - calculatedCompliance) > 0.01)
            {
                _logger.LogInformation($"CompliancePercentage mismatch for LogId {id}. Stored: {log.CompliancePercentage}%, Calculated: {calculatedCompliance}%");
                log.CompliancePercentage = calculatedCompliance;
                await _context.SaveChangesAsync();
            }

            CCTVLog = new CCTVLogViewModel
            {
                Id = log.Id,
                BranchId = log.BranchId,
                BranchName = (await _context.Branches.FirstOrDefaultAsync(b => b.Id == log.BranchId))?.BranchName ?? "Unknown Branch",
                OperatorName = log.OperatorName,
                ShiftType = log.ShiftType,
                Date = log.Date,
                Sections = null,
                CompliancePercentage = log.CompliancePercentage,
                Status = log.Status,
                Comments = log.Comments,
                BranchManagerNotes = log.BranchManagerNotes,
                ReportSeenBy = log.ReportSeenBy,
                BranchManager = log.BranchManager,
                DateOfReportSeen = log.DateOfReportSeen,
                SubmittedById = log.SubmittedById,
                AvailableBranches = await _context.Branches.ToListAsync(),
                SelectedBranchId = isSuperUser ? null : user.BranchId
            };

            if (!string.IsNullOrEmpty(log.ChecklistData))
            {
                try
                {
                    var checklist = JsonSerializer.Deserialize<Dictionary<string, ChecklistSection>>(log.ChecklistData);
                    if (checklist == null || !checklist.Any())
                    {
                        _logger.LogWarning($"ChecklistData has no sections for LogId {id}");
                        CCTVLog.Sections = new Dictionary<string, SectionData>();
                    }
                    else
                    {
                        CCTVLog.Sections = checklist.ToDictionary(
                            kvp => kvp.Key,
                            kvp => new SectionData
                            {
                                Questions = kvp.Value.Questions,
                                CROComments = kvp.Value.CROComments ?? new Dictionary<string, string>(),
                                ManagersActionTaken = kvp.Value.ManagersActionTaken ?? new Dictionary<string, string>()
                            }
                        );
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"Failed to deserialize ChecklistData for LogId {id}: {ex.Message}");
                    CCTVLog.Sections = new Dictionary<string, SectionData>();
                }
            }
            else
            {
                _logger.LogWarning($"ChecklistData for LogId {id} is null or empty");
                CCTVLog.Sections = new Dictionary<string, SectionData>();
            }

            foreach (var section in CCTVLog.Sections)
            {
                foreach (var question in section.Value.Questions)
                {
                    _logger.LogInformation($"OnGet - Section: {section.Key}, Question: {question.Key}, Answer: {question.Value}, CROComments: '{(section.Value.CROComments != null && section.Value.CROComments.ContainsKey(question.Key) ? section.Value.CROComments[question.Key] : "null")}', ManagersActionTaken: '{(section.Value.ManagersActionTaken != null && section.Value.ManagersActionTaken.ContainsKey(question.Key) ? section.Value.ManagersActionTaken[question.Key] : "null")}'");
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string username = User.FindFirst(ClaimTypes.Name)?.Value;
            User user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.BranchId == null)
            {
                return Unauthorized();
            }

            bool isSuperUser = user.TierLevel == 3;
            ViewData["IsSuperUser"] = isSuperUser;

            // Validate model state
            if (!ModelState.IsValid)
            {
                CCTVLog.AvailableBranches = await _context.Branches.ToListAsync();
                return Page();
            }

            // Validate BranchManager field
            if (string.IsNullOrEmpty(CCTVLog.BranchManager))
            {
                ModelState.AddModelError("CCTVLog.BranchManager", "The Branch Manager field is required.");
                CCTVLog.AvailableBranches = await _context.Branches.ToListAsync();
                return Page();
            }

            _logger.LogInformation($"Submitting review for LogId: {CCTVLog.Id}, User: {username}, BranchId: {user.BranchId}, IsSuperUser: {isSuperUser}");

            IQueryable<CCTVLog> logQuery = _context.CCTVLogs
                .Where(l => l.Id == CCTVLog.Id && l.Status == "Final" && l.ReviewedById == null);

            if (!isSuperUser)
            {
                logQuery = logQuery.Where(l => l.BranchId == user.BranchId);
            }

            var log = await logQuery.FirstOrDefaultAsync();

            if (log == null)
            {
                _logger.LogWarning($"Log not found in OnPostAsync. LogId: {CCTVLog.Id}, User: {username}, BranchId: {user.BranchId}, IsSuperUser: {isSuperUser}");
                return NotFound();
            }

            // Initialize CCTVLog.Sections if null or reconstruct from form data
            if (CCTVLog.Sections == null)
            {
                CCTVLog.Sections = new Dictionary<string, SectionData>();
            }

            // Reconstruct Sections from bound form data with bounds checking
            int sectionCount = SectionKeys?.Length ?? 0;
            for (int i = 0; i < sectionCount; i++)
            {
                var sectionKey = SectionKeys[i];
                if (!CCTVLog.Sections.ContainsKey(sectionKey))
                {
                    CCTVLog.Sections[sectionKey] = new SectionData
                    {
                        Questions = new Dictionary<string, string>(),
                        CROComments = new Dictionary<string, string>(),
                        ManagersActionTaken = new Dictionary<string, string>()
                    };
                }

                if (QuestionKeys != null && i < QuestionKeys.Length && QuestionKeys[i] != null)
                {
                    int questionCount = Math.Min(QuestionKeys[i].Length, QuestionValues?.Length > i && QuestionValues[i] != null ? QuestionValues[i].Length : 0);
                    for (int j = 0; j < questionCount; j++)
                    {
                        var questionKey = QuestionKeys[i][j];
                        CCTVLog.Sections[sectionKey].Questions[questionKey] = QuestionValues?[i][j] ?? "";

                        if (CROComments != null && i < CROComments.Length && CROComments[i] != null && j < CROComments[i].Length)
                        {
                            CCTVLog.Sections[sectionKey].CROComments[questionKey] = CROComments[i][j] ?? "";
                            _logger.LogInformation($"Section {sectionKey} - Question {questionKey} - Updated CROComments to: '{CROComments[i][j] ?? "null"}'");
                        }

                        if (ManagersActionTaken != null && i < ManagersActionTaken.Length && ManagersActionTaken[i] != null && j < ManagersActionTaken[i].Length)
                        {
                            CCTVLog.Sections[sectionKey].ManagersActionTaken[questionKey] = ManagersActionTaken[i][j] ?? "";
                            _logger.LogInformation($"Section {sectionKey} - Question {questionKey} - Updated ManagersActionTaken to: '{ManagersActionTaken[i][j] ?? "null"}'");
                        }
                    }
                }
            }

            var sections = CCTVLog.Sections.ToDictionary(
                kvp => kvp.Key,
                kvp => new ChecklistSection
                {
                    Questions = kvp.Value.Questions,
                    CROComments = kvp.Value.CROComments,
                    ManagersActionTaken = kvp.Value.ManagersActionTaken
                }
            );

            for (int i = 0; i < SectionKeys.Length; i++)
            {
                var sectionKey = SectionKeys[i];
                if (!sections.ContainsKey(sectionKey))
                {
                    sections[sectionKey] = new ChecklistSection
                    {
                        Questions = new Dictionary<string, string>(),
                        CROComments = new Dictionary<string, string>(),
                        ManagersActionTaken = new Dictionary<string, string>()
                    };
                }

                if (QuestionKeys != null && i < QuestionKeys.Length && QuestionKeys[i] != null)
                {
                    int croCommentsLength = CROComments?.Length > i && CROComments[i] != null ? CROComments[i].Length : 0;
                    int managersActionLength = ManagersActionTaken?.Length > i && ManagersActionTaken[i] != null ? ManagersActionTaken[i].Length : 0;
                    int questionCount = Math.Min(Math.Min(QuestionKeys[i].Length, croCommentsLength), managersActionLength);
                    for (int j = 0; j < questionCount; j++)
                    {
                        var questionKey = QuestionKeys[i][j];

                        if (sections[sectionKey].CROComments == null)
                            sections[sectionKey].CROComments = new Dictionary<string, string>();
                        if (sections[sectionKey].ManagersActionTaken == null)
                            sections[sectionKey].ManagersActionTaken = new Dictionary<string, string>();

                        if (CROComments != null && i < CROComments.Length && CROComments[i] != null && j < CROComments[i].Length)
                        {
                            sections[sectionKey].CROComments[questionKey] = CROComments[i][j] ?? "";
                            _logger.LogInformation($"Section {sectionKey} - Question {questionKey} - Updated CROComments to: '{CROComments[i][j] ?? "null"}'");
                        }

                        if (ManagersActionTaken != null && i < ManagersActionTaken.Length && ManagersActionTaken[i] != null && j < ManagersActionTaken[i].Length)
                        {
                            sections[sectionKey].ManagersActionTaken[questionKey] = ManagersActionTaken[i][j] ?? "";
                            _logger.LogInformation($"Section {sectionKey} - Question {questionKey} - Updated ManagersActionTaken to: '{ManagersActionTaken[i][j] ?? "null"}'");
                        }
                    }
                }
            }

            CCTVLog.Sections = sections.ToDictionary(
                kvp => kvp.Key,
                kvp => new SectionData
                {
                    Questions = kvp.Value.Questions,
                    CROComments = kvp.Value.CROComments,
                    ManagersActionTaken = kvp.Value.ManagersActionTaken
                }
            );

            foreach (var section in CCTVLog.Sections)
            {
                foreach (var question in section.Value.Questions.Keys)
                {
                    _logger.LogInformation($"OnPost - Section: {section.Key}, Question: {question}, Answer: {section.Value.Questions[question]}, CROComments: '{section.Value.CROComments[question]}', ManagersActionTaken: '{section.Value.ManagersActionTaken[question]}'");
                }
            }

            if (isSuperUser)
            {
                if (!CCTVLog.SelectedBranchId.HasValue)
                {
                    ModelState.AddModelError("CCTVLog.SelectedBranchId", "Please select a branch.");
                    CCTVLog.AvailableBranches = await _context.Branches.ToListAsync();
                    ViewData["IsSuperUser"] = isSuperUser;
                    return Page();
                }
                log.BranchId = CCTVLog.SelectedBranchId.Value;
            }

            log.BranchManagerNotes = CCTVLog.BranchManagerNotes;
            log.ReportSeenBy = CCTVLog.ReportSeenBy;
            log.BranchManager = CCTVLog.BranchManager;
            log.DateOfReportSeen = CCTVLog.DateOfReportSeen;
            log.ReviewedById = user.Id;
            log.Status = "Completed";
            log.ChecklistData = JsonSerializer.Serialize(sections);

            log.CompliancePercentage = CalculateCompliancePercentage(log.ChecklistData);
            _logger.LogInformation($"Updated CompliancePercentage for LogId {log.Id} after review: {log.CompliancePercentage}%");

            // Sync ChecklistViolations with updated ChecklistData
            _context.ChecklistViolations.RemoveRange(_context.ChecklistViolations.Where(cv => cv.CCTVLogId == log.Id));
            if (!string.IsNullOrEmpty(log.ChecklistData))
            {
                var checklist = JsonSerializer.Deserialize<Dictionary<string, ChecklistSection>>(log.ChecklistData);
                foreach (var section in checklist)
                {
                    foreach (var q in section.Value.Questions.Where(q => q.Value.ToLower() == "no"))
                    {
                        _context.ChecklistViolations.Add(new ChecklistViolation
                        {
                            CCTVLogId = log.Id,
                            Section = section.Key,
                            Question = q.Key
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("/CCTVLogsForReview");
        }

        private float CalculateCompliancePercentage(string checklistData)
        {
            if (string.IsNullOrEmpty(checklistData)) return 0;

            try
            {
                var checklist = JsonSerializer.Deserialize<Dictionary<string, ChecklistSection>>(checklistData);
                if (checklist == null || !checklist.Any()) return 0;

                int totalQuestions = 0;
                int yesAnswers = 0;

                foreach (var section in checklist)
                {
                    if (section.Value?.Questions == null) continue;

                    totalQuestions += section.Value.Questions.Count;
                    yesAnswers += section.Value.Questions.Count(q => q.Value == "Yes");
                }

                return totalQuestions > 0 ? (float)yesAnswers / totalQuestions * 100 : 0;
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Failed to calculate compliance percentage: {ex.Message}");
                return 0;
            }
        }
    }
}