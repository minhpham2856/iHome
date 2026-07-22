using System;
using System.Net;
using System.Net.Mail;

namespace iHome.BLL.Services
{
	// Send email via SMTP (Gmail) — credentials from env vars SMTP_EMAIL / SMTP_APP_PASSWORD
	public class EmailService
	{
		// Read sender address from environment; fallback placeholder for local dev without SMTP configured
		private readonly string _email = Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? "yourgmail@gmail.com";
		// Read Gmail app password from environment; fallback dummy value when env is missing
		private readonly string _appPassword = Environment.GetEnvironmentVariable("SMTP_APP_PASSWORD") ?? "xxxxxxxxxxxxxxxx";

		// Send newly created Manager account credentials to the manager's inbox
		public void SendLoginInfo(string toEmail, string username, string password)
		{
			try
			{
				// Build outbound message with Vietnamese body containing login credentials
				var mail = new MailMessage();

				// Set From header to configured SMTP sender address
				mail.From = new MailAddress(_email);
				// Add recipient — the manager email collected on the create form
				mail.To.Add(toEmail);
				// Subject line shown in the manager's inbox
				mail.Subject = "Thông tin tài khoản iHome";
				// Plain-text body: username + temporary password + prompt to change password after first login
				mail.Body =
					"Xin chào, tài khoản của bạn đã được tạo thành công.\n\n" +
					$"Tên đăng nhập: {username}\n" +
					$"Mật khẩu tạm thời: {password}\n\n" +
					"Vui lòng đổi mật khẩu sau lần đăng nhập đầu tiên.";

				// Gmail SMTP endpoint on submission port 587 (STARTTLS)
				var smtp = new SmtpClient("smtp.gmail.com", 587);
				// Enable TLS — required by Gmail for app-password auth
				smtp.EnableSsl = true;
				// Authenticate with sender email + app password from environment
				smtp.Credentials = new NetworkCredential(_email, _appPassword);

				// Transmit message — throws on network/auth failure
				smtp.Send(mail);
			}
			catch (Exception ex)
			{
				// Wrap raw SMTP exception as Vietnamese InvalidOperationException for UI MessageBox
				throw new InvalidOperationException("Không thể gửi email thông tin tài khoản. Kiểm tra cấu hình SMTP.", ex);
			}
		}

		// Send plaintext new password after ForgotPassword flow (hash already saved in DB)
		public void SendPasswordResetInfo(string toEmail, string newPassword)
		{
			try
			{
				// Build outbound reset notification message
				var mail = new MailMessage();

				// Set From header to configured SMTP sender address
				mail.From = new MailAddress(_email);
				// Deliver to the email address the user submitted on forgot-password form
				mail.To.Add(toEmail);
				// Subject line for password reset email
				mail.Subject = "Thông tin đặt lại mật khẩu iHome";
				// Body contains the new temporary password (already hashed in DB before this call)
				mail.Body =
					"Xin chào, mật khẩu của bạn đã được đặt lại thành công.\n\n" +
					$"Mật khẩu mới: {newPassword}\n\n" +
					"Vui lòng đổi mật khẩu sau lần đăng nhập đầu tiên.";

				// Gmail SMTP client on port 587
				var smtp = new SmtpClient("smtp.gmail.com", 587);
				// Require TLS for Gmail authentication
				smtp.EnableSsl = true;
				// Supply credentials from environment variables
				smtp.Credentials = new NetworkCredential(_email, _appPassword);
				// Send synchronously — failure propagates to AuthService caller
				smtp.Send(mail);
			}
			catch (Exception ex)
			{
				// Surface user-friendly Vietnamese error instead of raw SmtpException
				throw new InvalidOperationException("Không thể gửi email đặt lại mật khẩu. Kiểm tra cấu hình SMTP.", ex);
			}
		}
	}
}
