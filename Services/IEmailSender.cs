using System.Threading.Tasks;

namespace CampusSentinel.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
