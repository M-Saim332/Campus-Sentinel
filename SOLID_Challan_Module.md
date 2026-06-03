```csharp
namespace CampusSentinel.Models
{
    public class Challan
    {
        public int Id { get; set; }
        public string QrCodeId { get; set; }
        public string SubjectName { get; set; }
        public string SubjectType { get; set; }
        public string ViolationType { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public int IssuedByUserId { get; set; }
        public User IssuedBy { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public ChallanStatus Status { get; set; } = ChallanStatus.Pending;
        public string? Notes { get; set; }
    }
}
```

```csharp
namespace CampusSentinel.Models
{
    public enum ChallanStatus
    {
        Pending = 0,
        Paid = 1,
        Disputed = 2,
        Cancelled = 3
    }
}
```

```csharp
namespace CampusSentinel.Models
{
    public class ChallanSubjectDto
    {
        public string QrCodeId { get; set; }
        public string FullName { get; set; }
        public string SubjectType { get; set; }
        public string? Detail { get; set; }
    }
}
```

```csharp
using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Models
{
    public class IssueChallanRequest
    {
        [Required(ErrorMessage = "A QR code must be scanned before issuing a challan.")]
        [MaxLength(50)]
        public string QrCodeId { get; set; }

        [Required(ErrorMessage = "Violation type cannot be empty.")]
        [MaxLength(100, ErrorMessage = "Violation type cannot exceed 100 characters.")]
        public string ViolationType { get; set; }

        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "An amount is required.")]
        [Range(0.01, 1_000_000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public int IssuedByUserId { get; set; }
    }
}
```

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusSentinel.Models;

namespace CampusSentinel.Services
{
    public interface IChallanService
    {
        Task<ChallanSubjectDto?> ResolveQrCodeAsync(string qrCode);
        Task<(bool Success, int ChallanId, string Error)> IssueChallanAsync(IssueChallanRequest request);
        Task<IEnumerable<Challan>> GetAllChallansAsync();
        Task<Challan?> GetChallanByIdAsync(int id);
        Task<bool> UpdateStatusAsync(int challanId, ChallanStatus newStatus, string? notes);
    }
}
```

```csharp
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
    public class ChallanService : IChallanService
    {
        private const int MaxQrCodeLength = 100;
        private static readonly Regex SafeQrPattern = new Regex(@"^[A-Za-z0-9\-_\.]+$", RegexOptions.Compiled);
        private readonly ApplicationDbContext _db;

        public ChallanService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ChallanSubjectDto?> ResolveQrCodeAsync(string qrCode)
        {
            ValidateQrFormat(qrCode);

            var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.QrCodeId == qrCode);
            if (student != null)
            {
                return new ChallanSubjectDto
                {
                    QrCodeId = qrCode,
                    FullName = student.FullName,
                    SubjectType = "Student",
                    Detail = $"Reg# {student.RegistrationNo} | {student.Department}"
                };
            }

            var staff = await _db.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.StaffId == qrCode);
            if (staff != null)
            {
                return new ChallanSubjectDto
                {
                    QrCodeId = qrCode,
                    FullName = staff.FullName,
                    SubjectType = "Staff",
                    Detail = $"{staff.Designation}"
                };
            }

            var visitor = await _db.Visitors.AsNoTracking().FirstOrDefaultAsync(v => v.TemporaryQrCodeId == qrCode);
            if (visitor != null)
            {
                return new ChallanSubjectDto
                {
                    QrCodeId = qrCode,
                    FullName = visitor.FullName,
                    SubjectType = "Visitor",
                    Detail = $"Purpose: {visitor.Purpose}"
                };
            }

            return null;
        }

        public async Task<(bool Success, int ChallanId, string Error)> IssueChallanAsync(IssueChallanRequest request)
        {
            if (request == null) return (false, 0, "Request payload cannot be null.");
            if (string.IsNullOrWhiteSpace(request.QrCodeId)) return (false, 0, "QR Code is required.");
            if (string.IsNullOrWhiteSpace(request.ViolationType)) return (false, 0, "Violation type cannot be empty.");
            if (request.Amount <= 0) return (false, 0, "Amount must be greater than zero.");

            var subject = await ResolveQrCodeAsync(request.QrCodeId);
            if (subject == null) return (false, 0, "No person found for scanned QR code.");

            var officialExists = await _db.Users.AnyAsync(u => u.Id == request.IssuedByUserId);
            if (!officialExists) return (false, 0, "Issuing official not found.");

            var challan = new Challan
            {
                QrCodeId = request.QrCodeId,
                SubjectName = subject.FullName,
                SubjectType = subject.SubjectType,
                ViolationType = request.ViolationType.Trim(),
                Description = request.Description?.Trim(),
                Amount = request.Amount,
                IssuedByUserId = request.IssuedByUserId,
                IssueDate = DateTime.UtcNow,
                Status = ChallanStatus.Pending
            };

            await _db.Challans.AddAsync(challan);
            await _db.SaveChangesAsync();

            return (true, challan.Id, string.Empty);
        }

        public async Task<IEnumerable<Challan>> GetAllChallansAsync()
        {
            return await _db.Challans
                .AsNoTracking()
                .Include(c => c.IssuedBy)
                .OrderByDescending(c => c.IssueDate)
                .ToListAsync();
        }

        public async Task<Challan?> GetChallanByIdAsync(int id)
        {
            return await _db.Challans
                .AsNoTracking()
                .Include(c => c.IssuedBy)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> UpdateStatusAsync(int challanId, ChallanStatus newStatus, string? notes)
        {
            var challan = await _db.Challans.FindAsync(challanId);
            if (challan == null) return false;

            challan.Status = newStatus;
            if (!string.IsNullOrWhiteSpace(notes)) challan.Notes = notes.Trim();

            await _db.SaveChangesAsync();
            return true;
        }

        private static void ValidateQrFormat(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
                throw new ArgumentException("QR code cannot be empty.");
            if (qrCode.Length > MaxQrCodeLength)
                throw new ArgumentException("QR code is too long.");
            if (!SafeQrPattern.IsMatch(qrCode))
                throw new ArgumentException("QR code contains invalid characters.");
        }
    }
}
```

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusSentinel.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task SaveChangesAsync();
    }
}
```
