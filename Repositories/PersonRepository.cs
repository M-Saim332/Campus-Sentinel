using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;

namespace CampusSentinel.Repositories
{
    public class PersonRepository : IPersonRepository
    {
        private readonly ApplicationDbContext _context;

        public PersonRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Student> GetStudentByQrCodeAsync(string qrcode)
        {
            return await _context.Students.FirstOrDefaultAsync(s => s.QrCodeId == qrcode);
        }

        public async Task<Visitor> GetVisitorByTemporaryQrCodeAsync(string qrcode)
        {
            return await _context.Visitors.FirstOrDefaultAsync(v => v.TemporaryQrCodeId == qrcode);
        }
    }
}
