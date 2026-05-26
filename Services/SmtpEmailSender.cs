using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MimeKit;

namespace CampusSentinel.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var host = _config["Smtp:Host"];
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];
            var from = _config["Smtp:FromAddress"];

            if (string.IsNullOrEmpty(host)) return; // Don't send if not configured

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Campus Sentinel", from));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("plain") { Text = message };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                if (!string.IsNullOrEmpty(username))
                {
                    await client.AuthenticateAsync(username, password);
                }
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}
