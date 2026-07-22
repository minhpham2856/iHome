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
				var mail = new MailMessage();

				mail.From = new MailAddress(_email);
				mail.To.Add(toEmail);
				mail.Subject = "Thông tin tài khoản iHome";
				mail.Body =
					"Xin chào, tài khoản của bạn đã được tạo thành công.\n\n" +
					$"Tên đăng nhập: {username}\n" +
					$"Mật khẩu tạm thời: {password}\n\n" +
					"Vui lòng đổi mật khẩu sau lần đăng nhập đầu tiên.";

				var smtp = new SmtpClient("smtp.gmail.com", 587);
				smtp.EnableSsl = true;
				smtp.Credentials = new NetworkCredential(_email, _appPassword);

				smtp.Send(mail);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Không thể gửi email thông tin tài khoản. Kiểm tra cấu hình SMTP.", ex);
			}
		}

		// Send plaintext new password after ForgotPassword flow (hash already saved in DB)
		public void SendPasswordResetInfo(string toEmail, string newPassword)
		{
			try
			{
				var mail = new MailMessage();

				mail.From = new MailAddress(_email);
				mail.To.Add(toEmail);
				mail.Subject = "Thông tin đặt lại mật khẩu iHome";
				mail.Body =
					"Xin chào, mật khẩu của bạn đã được đặt lại thành công.\n\n" +
					$"Mật khẩu mới: {newPassword}\n\n" +
					"Vui lòng đổi mật khẩu sau lần đăng nhập đầu tiên.";

				var smtp = new SmtpClient("smtp.gmail.com", 587);
				smtp.EnableSsl = true;
				smtp.Credentials = new NetworkCredential(_email, _appPassword);
				smtp.Send(mail);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Không thể gửi email đặt lại mật khẩu. Kiểm tra cấu hình SMTP.", ex);
			}
		}
	}
}
