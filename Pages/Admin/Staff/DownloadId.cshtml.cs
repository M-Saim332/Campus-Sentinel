using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Services;

namespace CampusSentinel.Pages.Admin.Staff
{
    public class DownloadIdModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;

        public DownloadIdModel(ApplicationDbContext context, IPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();

            var pdfBytes = _pdfService.GenerateStaffIdCard(staff);
            string fileName = $"ID_Staff_{staff.FullName.Replace(" ", "_")}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
