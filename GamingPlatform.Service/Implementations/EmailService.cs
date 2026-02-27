using GamingPlatform.Domain.Email;
using GamingPlatform.Service.Interfaces;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Service.Implementations
{
    public class EmailService : IEmailSender
    {
        private readonly MailSettings _mailSettings;

        public EmailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }


        public async Task SendEmailAsync(EmailMessage message)
        {
            var email = new MimeMessage
            {
                Sender = new MailboxAddress(_mailSettings.SendersName, _mailSettings.SmtpUserName),
                Subject = message.Subject
            };

            email.From.Add(new MailboxAddress(_mailSettings.SendersName, _mailSettings.SmtpUserName));
            email.To.Add(new MailboxAddress(message.MailTo, message.MailTo));
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.Content };

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync(_mailSettings.SmtpServer, _mailSettings.SmtpServerPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_mailSettings.SmtpUserName, _mailSettings.SmtpPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        // This is what ASP.NET Identity calls
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new EmailMessage
            {
                MailTo = email,
                Subject = subject,
                Content = htmlMessage
            };

            await SendEmailAsync(message);
        }
    }
}
