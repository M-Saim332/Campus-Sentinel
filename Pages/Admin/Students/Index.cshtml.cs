using System;
using System.Collections.Generic;
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
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Student> Students { get; set; }

        public async Task OnGetAsync()
        {
            Students = await _context.Students.ToListAsync();
        }

        public async Task<IActionResult> OnPostToggleBlacklistAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                student.IsBlacklisted = !student.IsBlacklisted;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        /// <summary>
        /// GET /Admin/Students?handler=QrImage&barcodeId=2025-DS-129
        /// Returns a PNG QR code for the given barcodeId.
        /// </summary>
        public IActionResult OnGetQrImage(string barcodeId)
        {
            if (string.IsNullOrWhiteSpace(barcodeId))
                return BadRequest();

            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(barcodeId, QRCodeGenerator.ECCLevel.Q);
                var qrCode     = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20); // Higher resolution
                
                string fileName = $"QR_{barcodeId}.png";
                return File(qrBytes, "image/png", fileName);
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}
