using System.Threading.Tasks;
using CampusSentinel.Models;

namespace CampusSentinel.Services
{
    public interface IAuthService
    {
        Task<User> SignInAsync(string username, string password);
        Task<User> SignUpAsync(User user, string password);
    }
}
