using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace iHome.BLL.Services
{
	public class EmailService
	{
		private readonly string _email = "yourgmail@gmail.com";
		private readonly string _appPassword = "xxxxxxxxxxxxxxxx";

		public void SendLoginInfo(string toEmail, string username, string password)
		{
			try
			{
				var mail = new MailMessage();

				mail.From = new MailAddress(_email);
				mail.To.Add(toEmail);
				mail.Subject = "Thông tin tài khoản iHome";
				mail.Body = $"Xin chào, tài khoản của bạn đã được tạo thành công." +
					$"Username: {username}" +
					$"Password: {password}" +
					$"Vui lòng đổi mật khẩu sau lần đăng nhập đầu tiên.";

				var smtp = new SmtpClient("smtp.gmail.com", 587);
				smtp.EnableSsl = true;
				smtp.Credentials = new NetworkCredential(_email, _appPassword);

				smtp.Send(mail);
			}
			catch
			{
				throw new();
			}
		}

		public void SendPasswordResetInfo(string toEmail, string newPassword)
		{
			try
			{
				var mail = new MailMessage();

				mail.From = new MailAddress(_email);
				mail.To.Add(toEmail);
				mail.Subject = "Thông tin đặt lại mật khẩu iHome";
				mail.Body = $"Xin chào, mật khẩu của bạn đã được đặt lại thành công." +
					$"Mật khẩu mới: {newPassword}" +
					$"Vui lòng đổi mật khẩu sau lần đăng nhập đầu tiên.";

				var smtp = new SmtpClient("smtp.gmail.com", 587);
				smtp.EnableSsl = true;
				smtp.Credentials = new NetworkCredential(_email, _appPassword);
				smtp.Send(mail);
			}
			catch
			{
				throw new();
			}
		}
	}
}
