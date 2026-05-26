using System.Threading.Tasks;
using CampusSentinel.Models;

namespace CampusSentinel.Repositories
{
    public interface IPersonRepository
    {
        Task<Student> GetStudentByQrCodeAsync(string qrcode);
        Task<Visitor> GetVisitorByTemporaryQrCodeAsync(string qrcode);
    }
}
