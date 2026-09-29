using System.Net;
using System.Net.Mail;

namespace ShopSphere.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlMessage)
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(
                _configuration["EmailSettings:SmtpPort"] ?? "587");

            var smtpUsername =
                _configuration["EmailSettings:SmtpUsername"];

            var smtpPassword =
                _configuration["EmailSettings:SmtpPassword"];

            var senderEmail =
                _configuration["EmailSettings:SenderEmail"];

            var senderName =
                _configuration["EmailSettings:SenderName"]
                ?? "ShopSphere";

            if (string.IsNullOrWhiteSpace(smtpHost) ||
                string.IsNullOrWhiteSpace(smtpUsername) ||
                string.IsNullOrWhiteSpace(smtpPassword) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException(
                    "Email settings are not configured properly.");
            }

            using var message = new MailMessage();

            message.From = new MailAddress(
                senderEmail,
                senderName);

            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = htmlMessage;
            message.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(
                smtpHost,
                smtpPort)
            {
                Credentials = new NetworkCredential(
                    smtpUsername,
                    smtpPassword),

                EnableSsl = true
            };

            await smtpClient.SendMailAsync(message);
        }
    }
}