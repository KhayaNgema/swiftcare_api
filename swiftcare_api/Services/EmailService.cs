using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace swiftcare_api.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;

        public EmailService()
        {
            // ✅ Gmail SMTP Configuration
            _smtpServer = "smtp.gmail.com";
            _smtpPort = 587; // Use 465 if you want SSL instead of TLS
            _smtpUsername = "ngemakhayalethu@gmail.com"; // Replace with your Gmail address
            _smtpPassword = "mvax lmpx wgoz dvgb";     // ⚠️ Use a Gmail App Password, not your normal password
            _enableSsl = true;
        }

        // 📩 Send basic email
        public async Task SendEmailAsync(string to, string subject, string body, string senderName)
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = _enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUsername, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(to);

                try
                {
                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine("✅ Email sent successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to send email: {ex.Message}");
                    throw;
                }
            }
        }

        // 📎 Send email with attachments
        public async Task SendEmailWithAttachmentsAsync(string to, string subject, string body, List<string> attachmentPaths, string senderName)
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = _enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUsername, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(to);

                foreach (var path in attachmentPaths)
                {
                    if (File.Exists(path))
                    {
                        mailMessage.Attachments.Add(new Attachment(path));
                    }
                }

                try
                {
                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine("✅ Email with attachments sent successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to send email with attachments: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
