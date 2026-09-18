using System.Net;
using System.Net.Mail;

namespace EveryCare.Api.Services;

public sealed record EmailResult(bool Sent, string? Error);

public sealed class EmailService(IConfiguration configuration, ILogger<EmailService> logger)
{
    public async Task<EmailResult> SendPartnerApprovedAsync(string recipient, string fullName, CancellationToken cancellationToken)
    {
        var section = configuration.GetSection("Email:Smtp");
        if (!section.GetValue<bool>("Enabled")) return new(false, "SMTP chưa được cấu hình.");
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(section["FromAddress"] ?? section["Username"]!, section["FromName"] ?? "EveryCare"),
                Subject = "Hồ sơ đối tác EveryCare đã được chấp nhận",
                Body = $"Xin chào {fullName},\n\nHồ sơ đối tác của bạn đã được chấp nhận. Bạn có thể đăng nhập bằng số điện thoại đã đăng ký tại trang đối tác của EveryCare.\n\nTrân trọng,\nEveryCare",
                IsBodyHtml = false
            };
            message.To.Add(recipient);
            using var client = new SmtpClient(section["Host"] ?? "smtp.gmail.com", section.GetValue("Port", 587))
            {
                EnableSsl = section.GetValue("EnableSsl", true),
                Credentials = new NetworkCredential(section["Username"], section["Password"])
            };
            await client.SendMailAsync(message, cancellationToken);
            return new(true, null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Không thể gửi email duyệt hồ sơ tới {Recipient}", recipient);
            return new(false, exception.Message);
        }
    }
}
