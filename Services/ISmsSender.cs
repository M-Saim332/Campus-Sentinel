using System.Threading.Tasks;

namespace CampusSentinel.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
