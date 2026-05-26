using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using CampusSentinel.Data;
using CampusSentinel.Models;
using CampusSentinel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CampusSentinel.Pages.Admin.Challans
{
    /// <summary>
    /// Page model for the Challan issuance workflow.
    ///
    /// Two-step process (all on one page):
    ///   Step 1 — GET /Admin/Challans/Issue?qr={code}  → resolves the person
    ///             and populates the subject card (AJAX-friendly JSON handler).
    ///   Step 2 — POST /Admin/Challans/Issue            → validates + persists the
    ///             challan and returns JSON { success, challanId, error }.
    /// </summary>
    public class IssueModel : PageModel
    {
        private readonly IChallanService _challanService;
        private readonly ApplicationDbContext _db;

        public IssueModel(IChallanService challanService, ApplicationDbContext db)
        {
            _challanService = challanService;
            _db = db;
        }

        // ── Bound input model ────────────────────────────────────────────────

        /// <summary>The full challan form, bound on POST.</summary>
        [BindProperty]
        public IssueChallanRequest ChallanInput { get; set; } = new();

        // ── Display-only view data ───────────────────────────────────────────

        /// <summary>Populated on GET when a QR code query-string is present.</summary>
        public ChallanSubjectDto? ResolvedSubject { get; set; }

        /// <summary>Error message to show in the subject-resolution panel.</summary>
        public string? ResolveError { get; set; }

        /// <summary>Set after a successful POST to show the confirmation banner.</summary>
        public int? IssuedChallanId { get; set; }

        // ── Standard page violations ──────────────────────────────────────────

        /// <summary>
        /// Predefined violation categories shown in the drop-down.
        /// A free-text field is always available for "Other".
        /// </summary>
        public static readonly string[] ViolationTypes =
        {
            "Smoking in prohibited area",
            "Littering / Unauthorized waste disposal",
            "Parking in restricted zone",
            "After-hours campus access",
            "Noise disturbance",
            "Dress-code violation",
            "Damage to campus property",
            "Unauthorized vehicle on campus",
            "ID / QR Card misuse",
            "Other (specify in description)"
        };

        // ── GET: initial load ─────────────────────────────────────────────────

        /// <summary>
        /// Handles the initial page load and optional QR pre-resolution.
        /// If a <paramref name="qr"/> query-string value is supplied (e.g. after
        /// a redirect from the Scan page), the subject card is pre-populated.
        /// </summary>
        public async Task OnGetAsync(string? qr)
        {
            if (string.IsNullOrWhiteSpace(qr))
                return;

            var subject = await _challanService.ResolveQrCodeAsync(qr).ConfigureAwait(false);
            if (subject != null)
            {
                ResolvedSubject              = subject;
                ChallanInput.QrCodeId        = subject.QrCodeId;
            }
        }

        // ── GET handler: AJAX QR resolution ──────────────────────────────────

        /// <summary>
        /// Lightweight JSON endpoint called by the front-end scanner script
        /// after a QR code is decoded.
        ///
        /// Route:  GET /Admin/Challans/Issue?handler=Resolve&amp;qrCode={value}
        /// Returns: { success, name, subjectType, detail, qrCodeId }  OR  { success:false, error }
        /// </summary>
        public async Task<IActionResult> OnGetResolveAsync(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
                return new JsonResult(new { success = false, error = "No QR code provided." });

            try
            {
                var subject = await _challanService.ResolveQrCodeAsync(qrCode);

                if (subject == null)
                    return new JsonResult(new
                    {
                        success = false,
                        error   = $"No person found for QR code: {qrCode}"
                    });

                return new JsonResult(new
                {
                    success     = true,
                    qrCodeId    = subject.QrCodeId,
                    name        = subject.FullName,
                    subjectType = subject.SubjectType,
                    detail      = subject.Detail
                });
            }
            catch (System.ArgumentException ex)
            {
                // Invalid / malicious payload — return a safe error message
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // ── POST: issue the challan ───────────────────────────────────────────

        /// <summary>
        /// Validates the submitted form and issues the challan via the service.
        /// Returns a JSON response so the UI can show an inline confirmation
        /// without a full page reload.
        ///
        /// JSON response shape: { success, challanId?, error? }
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            // ── Resolve the current user's Id from the database using User.Identity.Name ──
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new
                {
                    success = false,
                    error   = "Unable to determine the issuing official. Please log in again."
                });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    error   = "Issuing official account not found in database."
                });
            }

            ChallanInput.IssuedByUserId = user.Id;

            // ── Model-state check (Data Annotation layer) ─────────────────────
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage ?? "Validation failed.";

                return new JsonResult(new { success = false, error = firstError });
            }

            // ── Service-level validation + persistence ────────────────────────
            var (success, challanId, error) = await _challanService.IssueChallanAsync(ChallanInput);

            if (!success)
                return new JsonResult(new { success = false, error });

            return new JsonResult(new { success = true, challanId });
        }
    }
}
