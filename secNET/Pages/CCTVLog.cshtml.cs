using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    [Authorize(Policy = "AtLeastTier1")]
    public class CCTVLogModel : PageModel
    {
        private readonly SecNETContext _context;
        private readonly ILogger<CCTVLogModel> _logger;

        public CCTVLogModel(SecNETContext context, ILogger<CCTVLogModel> logger)
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

        public SelectList Branches { get; set; }

        public async Task OnGetAsync()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.BranchId == null)
            {
                throw new InvalidOperationException("User or BranchId not found.");
            }

            bool isSuperUser = user.TierLevel == 3;
            ViewData["IsSuperUser"] = isSuperUser;
            ViewData["UserBranchId"] = user.BranchId;

            Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "BranchName", user.BranchId);

            CCTVLog = new CCTVLogViewModel
            {
                BranchId = user.BranchId.Value,
                Date = DateTime.Today,
                Sections = new Dictionary<string, SectionData>()
            };

            InitializeSections();
        }

        private void InitializeSections()
        {
            var generalStaff = new SectionData
            {
                Questions = new Dictionary<string, string>
                {
                    { "Uniforms worn at all times. This includes PPE, Mop caps, Sec equipment etc.", "No" },
                    { "Staff arrive on time, clock in/out properly, and do not clock for others.", "No" },
                    { "Staff are friendly, polite, make eye contact and smile when dealing with customers.", "No" },
                    { "No eating, drinking, or chewing inside the shop.", "No" },
                    { "Personal belongings kept in lockers.", "No" },
                    { "No cell phone usage, except for managers.", "No" },
                    { "No smoking in the workplace.", "No" },
                    { "Staff stay within assigned departments unless authorized.", "No" },
                    { "No unauthorized reserving or hiding of items for later purchase.", "No" },
                    { "Staff use staff cash cards and provide proof of purchase.", "No" },
                    { "Breaks are taken only with supervisor approval, and staff return on time.", "No" },
                    { "No personal calls during work hours, except during breaks.", "No" },
                    { "Proper hygiene and grooming maintained. This includes no jewelry in Serv. Dept.", "No" },
                    { "Exit through staff entrance only, except for office staff.", "No" }
                },
                CROComments = new Dictionary<string, string>(),
                ManagersActionTaken = new Dictionary<string, string>()
            };
            foreach (var question in generalStaff.Questions.Keys)
            {
                generalStaff.CROComments[question] = "";
                generalStaff.ManagersActionTaken[question] = "";
            }
            CCTVLog.Sections["General Staff Monitoring (Procedure / Policy)"] = generalStaff;

            var cashierPOS = new SectionData
            {
                Questions = new Dictionary<string, string>
                {
                    { "Cashiers remain attentive at tills; no chatting or leaving tills unattended.", "No" },
                    { "All cash handling follows procedure (no cash held during breaks, no tips accepted).", "No" },
                    { "Return slips are properly documented and signed.", "No" },
                    { "Supervisors conduct random till spot checks.", "No" },
                    { "Supervisors present during cash counts.", "No" },
                    { "ATM picks done daily; all tills and machines online.", "No" }
                },
                CROComments = new Dictionary<string, string>(),
                ManagersActionTaken = new Dictionary<string, string>()
            };
            foreach (var question in cashierPOS.Questions.Keys)
            {
                cashierPOS.CROComments[question] = "";
                cashierPOS.ManagersActionTaken[question] = "";
            }
            CCTVLog.Sections["Cashier & POS Monitoring (Procedure / Policy)"] = cashierPOS;

            var receivingStock = new SectionData
            {
                Questions = new Dictionary<string, string>
                {
                    { "No unauthorized access to the receiving area.", "No" },
                    { "Goods properly checked, weighed, and verified upon arrival.", "No" },
                    { "Receiving clerks document all shipments and sign claims with security.", "No" },
                    { "No goods leave the receiving area without completed documentation.", "No" },
                    { "Perishable items follow cold chain procedures.", "No" },
                    { "Claims area is clean, neat, and tidy.", "No" },
                    { "Waste is checked via documentation and disposed of properly and timeously.", "No" }
                },
                CROComments = new Dictionary<string, string>(),
                ManagersActionTaken = new Dictionary<string, string>()
            };
            foreach (var question in receivingStock.Questions.Keys)
            {
                receivingStock.CROComments[question] = "";
                receivingStock.ManagersActionTaken[question] = "";
            }
            CCTVLog.Sections["Receiving & Stock Handling (Procedure / Policy)"] = receivingStock;

            var managementSupervisor = new SectionData
            {
                Questions = new Dictionary<string, string>
                {
                    { "Daily floor walks conducted to check for policy adherence.", "No" },
                    { "Random spot checks on staff lockers.", "No" },
                    { "Staff schedules updated and visible.", "No" },
                    { "Supervisors monitor queue lengths and staff productivity.", "No" },
                    { "Alarm codes and shop keys secured and not left unattended.", "No" }
                },
                CROComments = new Dictionary<string, string>(),
                ManagersActionTaken = new Dictionary<string, string>()
            };
            foreach (var question in managementSupervisor.Questions.Keys)
            {
                managementSupervisor.CROComments[question] = "";
                managementSupervisor.ManagersActionTaken[question] = "";
            }
            CCTVLog.Sections["Management & Supervisor Duties (Procedure / Policy)"] = managementSupervisor;

            SectionKeys = CCTVLog.Sections.Keys.ToArray();
            QuestionKeys = CCTVLog.Sections.Values.Select(s => s.Questions.Keys.ToArray()).ToArray();
            QuestionValues = CCTVLog.Sections.Values.Select(s => s.Questions.Values.ToArray()).ToArray();
            CROComments = CCTVLog.Sections.Values.Select(s => s.CROComments.Values.ToArray()).ToArray();
            ManagersActionTaken = CCTVLog.Sections.Values.Select(s => s.ManagersActionTaken.Values.ToArray()).ToArray();
        }

        public async Task<IActionResult> OnPostSubmitLogAsync()
        {
            _logger.LogInformation("=== Entering OnPostSubmitLogAsync ===");

            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.BranchId == null)
            {
                _logger.LogInformation("User or BranchId not found.");
                return Unauthorized();
            }

            bool isSuperUser = user.TierLevel == 3;
            ViewData["IsSuperUser"] = isSuperUser;
            ViewData["UserBranchId"] = user.BranchId;

            if (SectionKeys == null || SectionKeys.Length == 0)
            {
                _logger.LogInformation("SectionKeys is null or empty.");
                ModelState.AddModelError("", "No sections were submitted. Please ensure the form is filled out correctly.");
                Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "BranchName", user.BranchId);

                if (CCTVLog == null)
                {
                    CCTVLog = new CCTVLogViewModel
                    {
                        BranchId = user.BranchId.Value,
                        Date = DateTime.Today,
                        Sections = new Dictionary<string, SectionData>()
                    };
                }
                else if (CCTVLog.Sections == null)
                {
                    CCTVLog.Sections = new Dictionary<string, SectionData>();
                }

                InitializeSections();
                return Page();
            }

            _logger.LogInformation("=== Raw Form Data Before Processing ===");
            _logger.LogInformation($"SectionKeys: [{string.Join(", ", SectionKeys ?? new string[] { "null" })}]");
            _logger.LogInformation($"QuestionKeys array length: {(QuestionKeys?.Length.ToString() ?? "0")}");
            for (int i = 0; i < (QuestionKeys?.Length ?? 0); i++)
            {
                _logger.LogInformation($"QuestionKeys[{i}]: [{string.Join(", ", QuestionKeys[i] ?? new string[] { "null" })}]");
            }
            _logger.LogInformation($"QuestionValues array length: {(QuestionValues?.Length.ToString() ?? "0")}");
            for (int i = 0; i < (QuestionValues?.Length ?? 0); i++)
            {
                if (QuestionValues[i] != null)
                {
                    _logger.LogInformation($"QuestionValues[{i}]: [{string.Join(", ", QuestionValues[i])}]");
                }
                else
                {
                    _logger.LogInformation($"QuestionValues[{i}]: null");
                }
            }
            _logger.LogInformation($"CROComments array length: {(CROComments?.Length.ToString() ?? "0")}");
            for (int i = 0; i < (CROComments?.Length ?? 0); i++)
            {
                if (CROComments[i] != null)
                {
                    _logger.LogInformation($"CROComments[{i}]: [{string.Join(", ", CROComments[i].Select((c, idx) => $"[{idx}]: '{c ?? "null"}'"))}]");
                }
                else
                {
                    _logger.LogInformation($"CROComments[{i}]: null");
                }
            }
            _logger.LogInformation($"ManagersActionTaken array length: {(ManagersActionTaken?.Length.ToString() ?? "0")}");
            for (int i = 0; i < (ManagersActionTaken?.Length ?? 0); i++)
            {
                if (ManagersActionTaken[i] != null)
                {
                    _logger.LogInformation($"ManagersActionTaken[{i}]: [{string.Join(", ", ManagersActionTaken[i].Select((m, idx) => $"[{idx}]: '{m ?? "null"}'"))}]");
                }
                else
                {
                    _logger.LogInformation($"ManagersActionTaken[{i}]: null");
                }
            }
            _logger.LogInformation("=== End of Raw Form Data ===");

            if (!ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is invalid.");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogInformation($"ModelState Error in {state.Key}: {error.ErrorMessage}");
                    }
                }

                Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "BranchName", user.BranchId);

                if (CCTVLog == null)
                {
                    CCTVLog = new CCTVLogViewModel
                    {
                        BranchId = user.BranchId.Value,
                        Date = DateTime.Today,
                        Sections = new Dictionary<string, SectionData>()
                    };
                }
                else if (CCTVLog.Sections == null)
                {
                    CCTVLog.Sections = new Dictionary<string, SectionData>();
                }

                InitializeSections();

                if (SectionKeys != null && SectionKeys.Length > 0)
                {
                    var sections = new Dictionary<string, SectionData>();
                    for (int i = 0; i < SectionKeys.Length; i++)
                    {
                        var sectionKey = SectionKeys[i];
                        var sectionData = CCTVLog.Sections.ContainsKey(sectionKey)
                            ? CCTVLog.Sections[sectionKey]
                            : new SectionData
                            {
                                Questions = new Dictionary<string, string>(),
                                CROComments = new Dictionary<string, string>(),
                                ManagersActionTaken = new Dictionary<string, string>()
                            };

                        if (QuestionKeys != null && i < QuestionKeys.Length && QuestionKeys[i] != null)
                        {
                            for (int j = 0; j < QuestionKeys[i].Length; j++)
                            {
                                var questionKey = QuestionKeys[i][j];
                                var answer = QuestionValues != null && i < QuestionValues.Length && QuestionValues[i] != null && j < QuestionValues[i].Length ? QuestionValues[i][j] : "No";
                                sectionData.Questions[questionKey] = answer;

                                if (!sectionData.CROComments.ContainsKey(questionKey))
                                    sectionData.CROComments[questionKey] = "";
                                if (!sectionData.ManagersActionTaken.ContainsKey(questionKey))
                                    sectionData.ManagersActionTaken[questionKey] = "";
                            }
                        }

                        if (CROComments != null && i < CROComments.Length && CROComments[i] != null)
                        {
                            for (int j = 0; j < CROComments[i].Length && j < QuestionKeys[i].Length; j++)
                            {
                                var questionKey = QuestionKeys[i][j];
                                sectionData.CROComments[questionKey] = CROComments[i][j] ?? "";
                            }
                        }
                        if (ManagersActionTaken != null && i < ManagersActionTaken.Length && ManagersActionTaken[i] != null)
                        {
                            for (int j = 0; j < ManagersActionTaken[i].Length && j < QuestionKeys[i].Length; j++)
                            {
                                var questionKey = QuestionKeys[i][j];
                                sectionData.ManagersActionTaken[questionKey] = ManagersActionTaken[i][j] ?? "";
                            }
                        }

                        sections[sectionKey] = sectionData;
                    }
                    CCTVLog.Sections = sections;
                }

                _logger.LogInformation("Returning page due to invalid ModelState.");
                return Page();
            }

            if (!isSuperUser)
            {
                CCTVLog.BranchId = user.BranchId.Value;
            }

            var updatedSections = new Dictionary<string, SectionData>();
            for (int i = 0; i < SectionKeys.Length; i++)
            {
                var sectionKey = SectionKeys[i];
                var sectionData = new SectionData
                {
                    Questions = new Dictionary<string, string>(),
                    CROComments = new Dictionary<string, string>(),
                    ManagersActionTaken = new Dictionary<string, string>()
                };

                if (QuestionKeys != null && i < QuestionKeys.Length && QuestionKeys[i] != null)
                {
                    for (int j = 0; j < QuestionKeys[i].Length; j++)
                    {
                        var questionKey = QuestionKeys[i][j];
                        var answer = QuestionValues != null && i < QuestionValues.Length && QuestionValues[i] != null && j < QuestionValues[i].Length ? QuestionValues[i][j] : "No";
                        sectionData.Questions[questionKey] = answer;
                        _logger.LogInformation($"Section {sectionKey} - Question {questionKey} - Answer: {answer}");

                        sectionData.CROComments[questionKey] = "";
                        sectionData.ManagersActionTaken[questionKey] = "";
                    }
                }

                if (CROComments != null && i < CROComments.Length && CROComments[i] != null)
                {
                    for (int j = 0; j < CROComments[i].Length && j < QuestionKeys[i].Length; j++)
                    {
                        var questionKey = QuestionKeys[i][j];
                        sectionData.CROComments[questionKey] = CROComments[i][j] ?? "";
                        _logger.LogInformation($"Section {sectionKey} - Question {questionKey} - CROComments set to: '{sectionData.CROComments[questionKey]}'");
                    }
                }
                if (ManagersActionTaken != null && i < ManagersActionTaken.Length && ManagersActionTaken[i] != null)
                {
                    for (int j = 0; j < ManagersActionTaken[i].Length && j < QuestionKeys[i].Length; j++)
                    {
                        var questionKey = QuestionKeys[i][j];
                        sectionData.ManagersActionTaken[questionKey] = ManagersActionTaken[i][j] ?? "";
                        _logger.LogInformation($"Section {sectionKey} - Question {questionKey} - ManagersActionTaken set to: '{sectionData.ManagersActionTaken[questionKey]}'");
                    }
                }

                updatedSections[sectionKey] = sectionData;
            }

            CCTVLog.Sections = updatedSections;

            int totalQuestions = CCTVLog.Sections.Values.Sum(section => section.Questions.Count);
            int answeredYes = CCTVLog.Sections.Values.Sum(section => section.Questions.Count(x => x.Value == "Yes"));
            CCTVLog.CompliancePercentage = totalQuestions > 0 ? (double)answeredYes / totalQuestions * 100 : 0;

            _logger.LogInformation($"Submitting log - Total Questions: {totalQuestions}, Answered Yes: {answeredYes}, Compliance Percentage: {CCTVLog.CompliancePercentage:F2}%");

            foreach (var section in CCTVLog.Sections)
            {
                foreach (var question in section.Value.Questions)
                {
                    _logger.LogInformation($"Section: {section.Key}, Question: {question.Key}, Answer: {question.Value}, CROComments: '{section.Value.CROComments[question.Key]}', ManagersActionTaken: '{section.Value.ManagersActionTaken[question.Key]}'");
                }
            }

            var cctvLog = new CCTVLog
            {
                BranchId = CCTVLog.BranchId,
                OperatorName = CCTVLog.OperatorName,
                ShiftType = CCTVLog.ShiftType,
                Date = CCTVLog.Date,
                ChecklistData = JsonSerializer.Serialize(CCTVLog.Sections),
                CompliancePercentage = CCTVLog.CompliancePercentage,
                Status = "Final",
                Comments = CCTVLog.Comments,
                BranchManagerNotes = CCTVLog.BranchManagerNotes,
                ReportSeenBy = CCTVLog.ReportSeenBy,
                BranchManager = CCTVLog.BranchManager,
                DateOfReportSeen = CCTVLog.DateOfReportSeen,
                SubmittedById = user.Id
            };

            _logger.LogInformation($"CCTVLog before saving: {JsonSerializer.Serialize(cctvLog)}");

            try
            {
                _context.CCTVLogs.Add(cctvLog);
                await _context.SaveChangesAsync();

                // Populate ChecklistViolations with "no" answers
                if (!string.IsNullOrEmpty(cctvLog.ChecklistData))
                {
                    var checklist = JsonSerializer.Deserialize<Dictionary<string, ChecklistSection>>(cctvLog.ChecklistData);
                    foreach (var section in checklist)
                    {
                        foreach (var q in section.Value.Questions.Where(q => q.Value.ToLower() == "no"))
                        {
                            _context.ChecklistViolations.Add(new ChecklistViolation
                            {
                                CCTVLogId = cctvLog.Id,
                                Section = section.Key,
                                Question = q.Key
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving CCTVLog to database.");
                ModelState.AddModelError("", "An error occurred while saving the log. Please try again.");
                return Page();
            }

            _logger.LogInformation("Redirecting to Index after successful submission.");
            return RedirectToPage("/Index");
        }
    }
}