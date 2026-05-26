using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;
using QRCoder;

namespace CampusSentinel.Pages.Admin.Staff
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
            StaffList = new List<CampusSentinel.Models.Staff>();
        }

        public IList<CampusSentinel.Models.Staff> StaffList { get; set; }

        public async Task OnGetAsync()
        {
            StaffList = await _context.Staff.ToListAsync();
        }

        public async Task<IActionResult> OnPostToggleBlacklistAsync(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff != null)
            {
                staff.IsBlacklisted = !staff.IsBlacklisted;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public IActionResult OnGetQrImage(string staffId)
        {
            if (string.IsNullOrWhiteSpace(staffId)) return BadRequest();
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(staffId, QRCodeGenerator.ECCLevel.Q);
                var qrCode     = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);
                return File(qrBytes, "image/png", $"QR_{staffId}.png");
            }
            catch { return StatusCode(500); }
        }
    }
}
