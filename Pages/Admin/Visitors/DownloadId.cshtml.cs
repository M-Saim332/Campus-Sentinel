using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Repositories;
using CampusSentinel.Services;

namespace CampusSentinel.Pages.Admin.Visitors
{
    public class DownloadIdModel : PageModel
    {
        private readonly IRepository<Visitor> _repository;
        private readonly IPdfService _pdfService;

        public DownloadIdModel(IRepository<Visitor> repository, IPdfService pdfService)
        {
            _repository = repository;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var visitor = await _repository.GetByIdAsync(id);
            if (visitor == null)
            {
                return NotFound();
            }

            var pdfBytes = _pdfService.GenerateVisitorIdCard(visitor);
            string fileName = $"ID_{visitor.FullName.Replace(" ", "_")}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
