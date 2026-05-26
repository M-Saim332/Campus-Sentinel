using System.Threading.Tasks;
using CampusSentinel.Models;

namespace CampusSentinel.Services
{
    public interface IVerificationService
    {
        Task<(bool IsGranted, string Reason, int LogId)> VerifyAccessAsync(string qrcode, string direction, int? guardId, string gateLocation);
        Task<bool> CheckBlacklistAsync(string qrcode);
        Task<Visitor> GenerateVisitorPassAsync(Visitor visitorInfo);
    }
}
