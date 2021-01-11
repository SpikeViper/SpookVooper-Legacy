using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace SpookVooper.Web.Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {
        private static string API_KEY = Secrets.EmailAPIKey;

        public EmailSender()
        {
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Execute(API_KEY, subject, message, email);
        }

        public Task Execute(string apiKey, string subject, string message, string email)
        {
            EmailAddress from = new EmailAddress("accounts@spookvooper.com", "SpikeViper");
            EmailAddress to = new EmailAddress(email);

            Console.WriteLine($"Sending email to {email}");

            var client = new SendGridClient(apiKey);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);

            return client.SendEmailAsync(msg);
        }
    }
}
