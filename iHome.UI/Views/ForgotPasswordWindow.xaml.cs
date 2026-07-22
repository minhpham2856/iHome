using System;
using System.Windows;
using System.Windows.Input;
using iHome.BLL.Services;
using iHome.DAL.Entities;

namespace iHome.UI.Views
{
	// Password recovery — temporary password emailed via AuthService + SMTP
	public partial class ForgotPasswordWindow : Window
	{
		public ForgotPasswordWindow()
		{
			InitializeComponent();
			txtEmail.Focus();
		}

		// Return to login
		private void lbReturnToLogin_MouseDown(object sender, MouseButtonEventArgs e)
		{
			new LoginWindow().Show();
			Close();
		}

		private void btnLogin_Click(object sender, RoutedEventArgs e)
		{
			RecoverPassword();
		}

		// Call ForgotPassword; on success return to LoginWindow
		private void RecoverPassword()
		{
			try
			{
				new AuthService().ForgotPassword(new User { Email = txtEmail.Text });
				MessageBox.Show(
					"Mật khẩu tạm đã được gửi tới email của bạn.",
					"Thành công",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
				new LoginWindow().Show();
				Close();
			}
			catch (ArgumentException ex)
			{
				// Unknown email or invalid input — show BLL message
				MessageBox.Show(ex.Message, "Không thể khôi phục", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			catch (Exception)
			{
				// SMTP/infra failure — keep details off the UI
				MessageBox.Show(
					"Không thể gửi email khôi phục. Kiểm tra cấu hình SMTP hoặc thử lại.",
					"Lỗi",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		// Same as lbReturnToLogin_MouseDown (Hyperlink variant)
		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			var loginWindow = new LoginWindow();
			loginWindow.Show();
			Close();
		}

		// Enter in email submits recovery
		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				RecoverPassword();
			}
		}
	}
}
