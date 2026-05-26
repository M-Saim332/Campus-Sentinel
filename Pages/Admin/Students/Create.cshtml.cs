using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;
using QRCoder;

namespace CampusSentinel.Pages.Admin.Students
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Student Student { get; set; }

        public string GeneratedQrCodeBase64 { get; set; }
        public string GeneratedBarcodeId { get; set; }

        public IActionResult OnGet()
        {
            Student = new Student { 
                Session = DateTime.Now.Year,
                Gender = "Male",
                ResidencyType = "Day Scholar"
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Student.QrCodeId");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Structured Barcode ID: Session-Dept-RegNo
            string dept = (Student.Department ?? "GEN").ToUpper().Trim();
            string regNo = (Student.RegistrationNo ?? "0").Trim();
            string barcodeId = $"{Student.Session}-{dept}-{regNo}";

            // Uniqueness across all verification categories
            var existingStudent = await _context.Students.AnyAsync(s => s.QrCodeId == barcodeId);
            var existingVisitor = await _context.Visitors.AnyAsync(v => v.TemporaryQrCodeId == barcodeId);
            var existingStaff   = await _context.Staff.AnyAsync(s => s.StaffId == barcodeId);

            if (existingStudent || existingVisitor || existingStaff)
            {
                ModelState.AddModelError(string.Empty,
                    $"Barcode ID '{barcodeId}' is already registered. Please check the Registration No.");
                return Page();
            }

            Student.QrCodeId = barcodeId;
            Student.CreatedAt = DateTime.Now;

            _context.Students.Add(Student);
            await _context.SaveChangesAsync();

            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(barcodeId, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);
                GeneratedQrCodeBase64 = Convert.ToBase64String(qrBytes);
                GeneratedBarcodeId = barcodeId;
            }
            catch { }

            ModelState.Clear();
            Student = new Student { Session = DateTime.Now.Year, Gender = "Male", ResidencyType = "Day Scholar" };
            return Page();
        }
    }
}
