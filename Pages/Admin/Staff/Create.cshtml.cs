using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;
using QRCoder;

namespace CampusSentinel.Pages.Admin.Staff
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CampusSentinel.Models.Staff Staff { get; set; }

        public string GeneratedQrCodeBase64 { get; set; }
        public string GeneratedStaffId { get; set; }

        public void OnGet()
        {
            Staff = new CampusSentinel.Models.Staff { Gender = "Male" };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Staff.StaffId");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // --- Generate Formatted Staff ID ---
            // Faculty: FR-12-IDS
            // Helper: HP-01-IDS
            // Gardener: GD-01-UET
            
            string prefix = Staff.Category switch
            {
                StaffCategory.Faculty => "FR",
                StaffCategory.Helper => "HP",
                StaffCategory.Gardener => "GD",
                _ => "WK"
            };

            int nextId = await _context.Staff.CountAsync() + 101; // Start from 101 for professional look
            string deptPart = (Staff.DepartmentOrUni ?? "UNI").ToUpper().Trim();
            string staffId = $"{prefix}-{nextId}-{deptPart}";

            // Ensure uniqueness
            var existing = await _context.Staff.AnyAsync(s => s.StaffId == staffId);
            if (existing) staffId += "A"; // Simple collision avoidance

            Staff.StaffId = staffId;
            Staff.CreatedAt = DateTime.Now;

            _context.Staff.Add(Staff);
            await _context.SaveChangesAsync();

            // QR Generation
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(staffId, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);
                GeneratedQrCodeBase64 = Convert.ToBase64String(qrBytes);
                GeneratedStaffId = staffId;
            }
            catch { }

            ModelState.Clear();
            Staff = new CampusSentinel.Models.Staff { Gender = "Male" };
            return Page();
        }
    }
}
