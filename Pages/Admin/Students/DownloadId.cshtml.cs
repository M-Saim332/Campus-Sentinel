using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Repositories;
using CampusSentinel.Services;

namespace CampusSentinel.Pages.Admin.Students
{
    public class DownloadIdModel : PageModel
    {
        private readonly IRepository<Student> _repository;
        private readonly IPdfService _pdfService;

        public DownloadIdModel(IRepository<Student> repository, IPdfService pdfService)
        {
            _repository = repository;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var student = await _repository.GetByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var pdfBytes = _pdfService.GenerateStudentIdCard(student);
            string fileName = $"ID_{student.FullName.Replace(" ", "_")}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
