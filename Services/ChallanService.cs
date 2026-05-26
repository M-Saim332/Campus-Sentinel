using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CampusSentinel.Data;
using CampusSentinel.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusSentinel.Services
{
    /// <summary>
    /// Concrete implementation of <see cref="IChallanService"/>.
    ///
    /// Design notes:
    ///   • The "this" keyword is deliberately omitted throughout (clean-code standard).
    ///   • All database interaction is done via <see cref="ApplicationDbContext"/> to
    ///     stay consistent with the existing VerificationService / IncidentService patterns.
    ///   • QR code resolution uses the same three-table lookup already present in
    ///     VerificationService — Students → Staff → Visitors — so that no new query
    ///     patterns are introduced.
    ///   • Input validation happens at two levels:
    ///       1. Format / security: ResolveQrCodeAsync (rejects empty, over-long,
    ///          or suspicious injection-like strings).
    ///       2. Business rules: IssueChallanAsync (amount > 0, person exists, etc.).
    /// </summary>
    public class ChallanService : IChallanService
    {
        // ── Maximum accepted raw length for any QR code string ────────────────
        private const int MaxQrCodeLength = 100;

        // ── Regex: only allow alphanumeric, dash, underscore, and dot ─────────
        // Blocks SQL-injection vectors such as quotes, semicolons, angle-brackets,
        // and common command injection characters.
        private static readonly Regex SafeQrPattern =
            new Regex(@"^[A-Za-z0-9\-_\.]+$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

        private readonly ApplicationDbContext _db;

        public ChallanService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ── 1. QR Code Resolution ─────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<ChallanSubjectDto?> ResolveQrCodeAsync(string qrCode)
        {
            // ── Format & security validation ──────────────────────────────────
            ValidateQrFormat(qrCode);   // throws ArgumentException on bad input

            // ── Multi-table lookup (Students → Staff → Visitors) ──────────────
            // Exactly mirrors the lookup order used by VerificationService so
            // both systems stay consistent without coupling.

            var student = await _db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.QrCodeId == qrCode);

            if (student != null)
            {
                return new ChallanSubjectDto
                {
                    QrCodeId    = qrCode,
                    FullName    = student.FullName,
                    SubjectType = "Student",
                    Detail      = $"Reg# {student.RegistrationNo} | {student.Department} | {student.Session}"
                };
            }

            var staff = await _db.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StaffId == qrCode);

            if (staff != null)
            {
                return new ChallanSubjectDto
                {
                    QrCodeId    = qrCode,
                    FullName    = staff.FullName,
                    SubjectType = "Staff",
                    Detail      = $"{staff.Designation} | {staff.DepartmentOrUni}"
                };
            }

            var visitor = await _db.Visitors
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.TemporaryQrCodeId == qrCode);

            if (visitor != null)
            {
                return new ChallanSubjectDto
                {
                    QrCodeId    = qrCode,
                    FullName    = visitor.FullName,
                    SubjectType = "Visitor",
                    Detail      = $"Purpose: {visitor.Purpose ?? "N/A"}"
                };
            }

            // No match found — return null; callers render a "User Not Found" response.
            return null;
        }

        // ── 2. Issue Challan ──────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<(bool Success, int ChallanId, string Error)> IssueChallanAsync(
            IssueChallanRequest request)
        {
            // ── Server-side business-rule validation ──────────────────────────

            if (request == null)
                return (false, 0, "Request payload cannot be null.");

            if (string.IsNullOrWhiteSpace(request.QrCodeId))
                return (false, 0, "QR Code is required.");

            if (string.IsNullOrWhiteSpace(request.ViolationType))
                return (false, 0, "Violation type cannot be empty.");

            if (request.ViolationType.Length > 100)
                return (false, 0, "Violation type cannot exceed 100 characters.");

            if (request.Amount <= 0)
                return (false, 0, "Challan amount must be greater than zero (PKR).");

            if (request.Amount > 1_000_000)
                return (false, 0, "Challan amount exceeds the allowed maximum (PKR 1,000,000).");

            // ── Validate that the QR code maps to a real person ───────────────
            ChallanSubjectDto? subject = null;
            try
            {
                subject = await ResolveQrCodeAsync(request.QrCodeId);
            }
            catch (ArgumentException ex)
            {
                return (false, 0, ex.Message);
            }

            if (subject == null)
                return (false, 0, $"No student, staff member, or visitor found for QR code '{request.QrCodeId}'.");

            // ── Validate the issuing official exists ──────────────────────────
            var officialExists = await _db.Users.AnyAsync(u => u.Id == request.IssuedByUserId);
            if (!officialExists)
                return (false, 0, "Issuing official account not found.");

            // ── Persist the challan ───────────────────────────────────────────
            var challan = new Challan
            {
                QrCodeId       = request.QrCodeId,
                SubjectName    = subject.FullName,
                SubjectType    = subject.SubjectType,
                ViolationType  = request.ViolationType.Trim(),
                Description    = string.IsNullOrWhiteSpace(request.Description)
                                     ? null
                                     : request.Description.Trim(),
                Amount         = request.Amount,
                IssuedByUserId = request.IssuedByUserId,
                IssueDate      = DateTime.UtcNow,
                Status         = ChallanStatus.Pending
            };

            await _db.Challans.AddAsync(challan);
            await _db.SaveChangesAsync();

            return (true, challan.Id, string.Empty);
        }

        // ── 3. Query challans ─────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<IEnumerable<Challan>> GetAllChallansAsync()
        {
            return await _db.Challans
                .AsNoTracking()
                .Include(c => c.IssuedBy)
                .OrderByDescending(c => c.IssueDate)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Challan?> GetChallanByIdAsync(int id)
        {
            return await _db.Challans
                .AsNoTracking()
                .Include(c => c.IssuedBy)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // ── 4. Status update ──────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<bool> UpdateStatusAsync(int challanId, ChallanStatus newStatus, string? notes)
        {
            var challan = await _db.Challans.FindAsync(challanId);
            if (challan == null) return false;

            challan.Status = newStatus;

            if (!string.IsNullOrWhiteSpace(notes))
                challan.Notes = notes.Trim();

            await _db.SaveChangesAsync();
            return true;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Validates that a raw QR code string is neither empty, over-long,
        /// nor contains characters that could indicate a malicious payload
        /// (SQL injection fragments, HTML tags, shell metacharacters, etc.).
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown with a specific message when the input fails any rule.
        /// </exception>
        private static void ValidateQrFormat(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
                throw new ArgumentException("QR code payload cannot be empty.");

            if (qrCode.Length > MaxQrCodeLength)
                throw new ArgumentException(
                    $"QR code payload exceeds the maximum allowed length of {MaxQrCodeLength} characters. " +
                    "This may indicate a malformed or malicious code.");

            // Reject anything that is not alphanumeric/dash/underscore/dot.
            // This blocks SQL injection strings, HTML, JS, shell commands, etc.
            if (!SafeQrPattern.IsMatch(qrCode))
                throw new ArgumentException(
                    "QR code contains invalid characters. " +
                    "Only letters, digits, hyphens, underscores, and dots are accepted.");
        }
    }
}
