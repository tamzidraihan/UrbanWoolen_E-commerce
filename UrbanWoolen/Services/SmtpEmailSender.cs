using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace UrbanWoolen.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Read & normalize config
            var host = (_config["Email:Smtp:Host"] ?? "").Trim();
            var port = int.TryParse(_config["Email:Smtp:Port"], out var p) ? p : 587;

            // Username must be a valid FROM email address
            var usernameRaw = _config["Email:Smtp:Username"] ?? "";
            var username = usernameRaw.Trim();

            // Gmail app passwords are shown with spaces in UI; remove any spaces just in case
            var passwordRaw = _config["Email:Smtp:Password"] ?? "";
            var password = passwordRaw.Replace(" ", "");

            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("SMTP host is not configured.");
            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("SMTP username (From address) is not configured.");
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("SMTP password is not configured.");

            // Validate/normalize addresses early so we fail with a clear message
            var from = new MailAddress(username, "UrbanWoolen");
            var to = new MailAddress((email ?? string.Empty).Trim());

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password)
            };

            using var message = new MailMessage(from, to)
            {
                Subject = subject ?? string.Empty,
                Body = htmlMessage ?? string.Empty,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            await client.SendMailAsync(message);
        }
    }
}
