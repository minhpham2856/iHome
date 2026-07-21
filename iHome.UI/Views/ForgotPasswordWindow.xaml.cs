using System;
using System.Windows;
using System.Windows.Input;
using iHome.BLL.Services;

namespace iHome.UI.Views
{
	public partial class ForgotPasswordWindow : Window
	{
		public ForgotPasswordWindow()
		{
			InitializeComponent();
			txtEmail.Focus();
		}

		private void tbReturnToLogin_MouseDown(object sender, MouseButtonEventArgs e)
		{
			new LoginWindow().Show();
			Close();
		}

		private void btnLogin_Click(object sender, RoutedEventArgs e)
		{
			RecoverPassword();
		}

		private void RecoverPassword()
		{
			try
			{
				new AuthService().ForgotPassword(txtEmail.Text);
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
				MessageBox.Show(ex.Message, "Không thể khôi phục", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			catch (Exception)
			{
				MessageBox.Show(
					"Không thể gửi email khôi phục. Kiểm tra cấu hình SMTP hoặc thử lại.",
					"Lỗi",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			var loginWindow = new LoginWindow();
			loginWindow.Show();
			Close();
		}

		// key down enter
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
