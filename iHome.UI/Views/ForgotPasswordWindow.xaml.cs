using System;
using System.Windows;
using System.Windows.Input;
using iHome.BLL.Services;
using iHome.DAL.Entities;

namespace iHome.UI.Views
{
	// cửa sổ khôi phục mật khẩu — gửi mật khẩu tạm qua email SMTP (AuthService + EmailService)
	public partial class ForgotPasswordWindow : Window
	{
		public ForgotPasswordWindow()
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// focus sẵn vào email để nhập nhanh
			txtEmail.Focus();
		}

		// link quay lại đăng nhập — mở LoginWindow và đóng màn hình hiện tại
		private void lbReturnToLogin_MouseDown(object sender, MouseButtonEventArgs e)
		{
			// Open window non-modally for navigation flow
			new LoginWindow().Show();
			// Close current window after navigation or logout
			Close();
		}

		// nút Gửi / Khôi phục — ủy quyền cho RecoverPassword
		private void btnLogin_Click(object sender, RoutedEventArgs e)
		{
			// Call helper RecoverPassword to refresh UI state from BLL data
			RecoverPassword();
		}

		// luồng chính: gọi AuthService.ForgotPassword với email từ form; thành công quay về LoginWindow
		private void RecoverPassword()
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Instantiate AuthService for login, logout, or password recovery
				new AuthService().ForgotPassword(new User { Email = txtEmail.Text });
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(
					// Execute UI step inside RecoverPassword
					"Mật khẩu tạm đã được gửi tới email của bạn.",
					// Execute UI step inside RecoverPassword
					"Thành công",
					// Execute UI step inside RecoverPassword
					MessageBoxButton.OK,
					// Execute UI step inside RecoverPassword
					MessageBoxImage.Information);
				// Open window non-modally for navigation flow
				new LoginWindow().Show();
				// Close current window after navigation or logout
				Close();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (ArgumentException ex)
			{
				// email không tồn tại hoặc dữ liệu không hợp lệ — hiển thị message từ BLL
				MessageBox.Show(ex.Message, "Không thể khôi phục", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception)
			{
				// lỗi SMTP hoặc lỗi hạ tầng — không lộ chi tiết nội bộ cho người dùng
				MessageBox.Show(
					// Execute UI step inside RecoverPassword
					"Không thể gửi email khôi phục. Kiểm tra cấu hình SMTP hoặc thử lại.",
					// Execute UI step inside RecoverPassword
					"Lỗi",
					// Execute UI step inside RecoverPassword
					MessageBoxButton.OK,
					// Execute UI step inside RecoverPassword
					MessageBoxImage.Warning);
			}
		}

		// Hyperlink quay lại đăng nhập — tương đương lbReturnToLogin_MouseDown
		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			// Create navigation window (login, dashboard, forgot password)
			var loginWindow = new LoginWindow();
			// Open window non-modally for navigation flow
			loginWindow.Show();
			// Close current window after navigation or logout
			Close();
		}

		// Enter ở ô email — submit khôi phục mật khẩu
		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (e.Key == Key.Enter)
			{
				// Consume Enter key so WPF does not trigger default button twice
				e.Handled = true;
				// Call helper RecoverPassword to refresh UI state from BLL data
				RecoverPassword();
			}
		}
	}
}
