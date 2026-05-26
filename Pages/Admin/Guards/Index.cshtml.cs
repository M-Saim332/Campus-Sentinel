using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;

namespace CampusSentinel.Pages.Admin.Guards
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<SecurityGuard> Guards { get; set; }

        public async Task OnGetAsync()
        {
            Guards = await _context.SecurityGuards.ToListAsync();
        }
    }
}
