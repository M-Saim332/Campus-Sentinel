using System;
using System.Threading.Tasks;

namespace CampusSentinel.Services
{
    public class StubSmsSender : ISmsSender
    {
        public Task SendSmsAsync(string number, string message)
        {
            // Placeholder for real SMS gateway (e.g. Twilio)
            Console.WriteLine($"SMS SENT to {number}: {message}");
            return Task.CompletedTask;
        }
    }
}
