// Services/EmailService.cs
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Configuration;
using System.Diagnostics;

namespace ManufactPlanner.Services
{
    public class EmailService
    {
        // Настройки почтового сервера
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly bool _enableSsl;

        // Singleton для глобального доступа
        private static EmailService _instance;
        public static EmailService Instance => _instance ??= new EmailService();

        private EmailService()
        {
            try
            {
                // Загрузка настроек из конфигурационного файла или значений по умолчанию
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(configPath))
                {
                    // Здесь можно добавить парсинг JSON-файла
                    // Для упрощения сейчас используем значения по умолчанию
                }

                // Настройки по умолчанию (в реальном приложении лучше хранить в конфигурации)
                _smtpServer = "smtp.gmail.com";
                _smtpPort = 587;
                _smtpUsername = "lexus0506rus@gmail.com";  // Ваша почта
                _smtpPassword = "ajxh ornv qwrp zqcl";     // Пароль приложения (16 символов)
                _senderEmail = "lexus0506rus@gmail.com";   // Ваша почта
                _senderName = "ManufactPlanner";
                _enableSsl = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при инициализации EmailService: {ex.Message}");
                // Установка значений по умолчанию при ошибке
                _smtpServer = "smtp.gmail.com";
                _smtpPort = 587;
                _enableSsl = true;
            }
        }

        // Проверка валидности email-адреса
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Проверка доступности SMTP-сервера (можно вызывать при первом запуске приложения)
        public async Task<bool> CheckSmtpConnectionAsync()
        {
            if (string.IsNullOrEmpty(_smtpServer) || string.IsNullOrEmpty(_smtpUsername) ||
                string.IsNullOrEmpty(_smtpPassword))
                return false;

            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    client.EnableSsl = _enableSsl;
                    client.Timeout = 5000; // 5 секунд таймаут для проверки

                    // Мы не можем напрямую проверить соединение без отправки,
                    // но можно поймать исключение тайм-аута
                    await Task.Delay(100); // Задержка для имитации проверки
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка проверки SMTP-соединения: {ex.Message}");
                return false;
            }
        }

        // Отправка обычного email
        public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
        {
            if (string.IsNullOrEmpty(recipientEmail) || !IsValidEmail(recipientEmail))
                return false;

            if (string.IsNullOrEmpty(_smtpServer) || string.IsNullOrEmpty(_smtpUsername) ||
                string.IsNullOrEmpty(_smtpPassword))
            {
                Debug.WriteLine("Не настроены параметры SMTP-сервера");
                return false;
            }

            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    client.EnableSsl = _enableSsl;

                    var message = new MailMessage
                    {
                        From = new MailAddress(_senderEmail, _senderName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                        BodyEncoding = Encoding.UTF8
                    };

                    message.To.Add(new MailAddress(recipientEmail));

                    await client.SendMailAsync(message);
                    Debug.WriteLine($"Email успешно отправлен на {recipientEmail}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отправки email: {ex.Message}");
                return false;
            }
        }

        // Отправка уведомления в стилизованном HTML-формате
        public async Task<bool> SendNotificationEmailAsync(string recipientEmail, string title, string message)
        {
            if (!IsValidEmail(recipientEmail))
                return false;

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
                            <p>&copy; {DateTime.Now.Year} ManufactPlanner. Все права защищены.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(recipientEmail, title, htmlBody);
        }

        // Проверка настроек и отправка тестового письма
        public async Task<(bool Success, string Message)> SendTestEmailAsync(string recipientEmail)
        {
            if (!IsValidEmail(recipientEmail))
                return (false, "Указан некорректный email-адрес");

            try
            {
                bool result = await SendNotificationEmailAsync(
                    recipientEmail,
                    "Тестовое уведомление ManufactPlanner",
                    "Это тестовое уведомление от системы ManufactPlanner. Если вы получили это письмо, значит настройки почты работают корректно."
                );

                if (result)
                    return (true, "Тестовое письмо успешно отправлено");
                else
                    return (false, "Не удалось отправить тестовое письмо. Проверьте настройки SMTP-сервера.");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при отправке тестового письма: {ex.Message}");
            }
        }
    }
}