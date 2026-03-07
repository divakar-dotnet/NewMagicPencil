using MailKit.Net.Smtp;
using MimeKit;

namespace NewMagicPencil.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config) => _config = config;

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Magic Pencil", _config["Email:From"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Magic Pencil - Your OTP Code";
            message.Body = new TextPart("html")
            {
                Text = "<div style='font-family:Segoe UI,sans-serif;max-width:500px;margin:auto;'>" +
                       "<h2 style='color:#6c5ce7;'>Magic Pencil</h2>" +
                       "<p>You requested a password reset. Use the OTP below:</p>" +
                       "<div style='background:#f4f7f6;padding:20px;text-align:center;border-radius:10px;margin:20px 0;'>" +
                       "<h1 style='color:#6c5ce7;letter-spacing:8px;font-size:36px;'>" + otp + "</h1>" +
                       "</div>" +
                       "<p style='color:#999;'>This OTP is valid for <strong>5 minutes</strong>.</p>" +
                       "<p style='color:#999;'>If you did not request this, ignore this email.</p>" +
                       "</div>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_config["Email:SmtpHost"],
                int.Parse(_config["Email:SmtpPort"]),
                MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}