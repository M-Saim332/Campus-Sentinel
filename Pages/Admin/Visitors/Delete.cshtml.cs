using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Repositories;

namespace CampusSentinel.Pages.Admin.Visitors
{
    public class DeleteModel : PageModel
    {
        private readonly IRepository<Visitor> _repository;

        public DeleteModel(IRepository<Visitor> repository)
        {
            _repository = repository;
        }

        [BindProperty]
        public Visitor Visitor { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Visitor = await _repository.GetByIdAsync(id);

            if (Visitor == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var visitorToDelete = await _repository.GetByIdAsync(Visitor.Id);
            
            if (visitorToDelete != null)
            {
                _repository.Delete(visitorToDelete);
                await _repository.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
