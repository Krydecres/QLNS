using System.Net;
using System.Net.Mail;

namespace QLNS.FullNet.Web.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var host = _configuration["EmailSettings:Host"];
                var portStr = _configuration["EmailSettings:Port"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var displayName = _configuration["EmailSettings:DisplayName"];

                // Fallback nếu không có cấu hình thì log ra console (Mock)
                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Chưa cấu hình SMTP trong appsettings.json. Bỏ qua việc gửi email thật.");
                    _logger.LogInformation($"[MOCK EMAIL] Gửi đến: {toEmail} | Tiêu đề: {subject} | Nội dung: {body.Substring(0, Math.Min(body.Length, 100))}...");
                    return;
                }

                int.TryParse(portStr, out int port);

                using var client = new SmtpClient(host, port > 0 ? port : 587)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? username!, displayName ?? "Phòng Nhân Sự QLNS"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml,
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Đã gửi email thành công tới {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email tới {toEmail}: {ex.Message}");
            }
        }
    }
}
