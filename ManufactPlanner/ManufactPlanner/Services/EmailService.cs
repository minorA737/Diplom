// Services/EmailService.cs
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Text;

namespace ManufactPlanner.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;

        public EmailService(string smtpServer = "smtp.gmail.com", int smtpPort = 587,
            string smtpUsername = "manufactplanner@gmail.com", string smtpPassword = "your_app_password",
            string senderEmail = "manufactplanner@gmail.com")
        {
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _smtpUsername = smtpUsername;
            _smtpPassword = smtpPassword;
            _senderEmail = senderEmail;
        }

        public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
        {
            if (string.IsNullOrEmpty(recipientEmail))
                return false;

            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    EnableSsl = true
                };

                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "ManufactPlanner"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8
                };

                message.To.Add(new MailAddress(recipientEmail));

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отправки email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendNotificationEmailAsync(string recipientEmail, string title, string message)
        {
            string htmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <title>{title}</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; background-color: #f8f9fa; padding: 20px; border-radius: 5px; }}
                        .header {{ background-color: #00ACC1; color: white; padding: 10px; border-radius: 5px 5px 0 0; text-align: center; }}
                        .content {{ padding: 20px; background-color: white; border-radius: 0 0 5px 5px; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>{title}</h2>
                        </div>
                        <div class='content'>
                            <p>{message}</p>
                            <p>Это автоматическое уведомление от системы ManufactPlanner. Пожалуйста, не отвечайте на это письмо.</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2025 ManufactPlanner. Все права защищены.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(recipientEmail, title, htmlBody);
        }
    }
}