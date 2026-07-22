using System.Windows;
using System.Windows.Input;
using iHome.BLL.Services;

namespace iHome.UI.Views
{
	// cửa sổ đăng nhập — điểm vào ứng dụng sau App.xaml.cs; xác thực qua AuthService rồi mở DashboardWindow
	public partial class LoginWindow : Window
	{
		public LoginWindow()
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// đặt con trỏ sẵn vào ô tài khoản để người dùng gõ ngay
			txtUsername.Focus();
		}

		// TextBlock "Quên mật khẩu?" — mở ForgotPasswordWindow và đóng màn hình đăng nhập
		private void lbForgotPassword_MouseDown(object sender, MouseButtonEventArgs e)
		{
			// Open window non-modally for navigation flow
			new ForgotPasswordWindow().Show();
			// Close current window after navigation or logout
			Close();
		}

		// nút Đăng nhập — gọi BLL Login; thành công thì chuyển sang shell DashboardWindow với User đã xác thực
		private void btnLogin_Click(object sender, RoutedEventArgs e)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			string username = txtUsername.Text;
			// Read or clear PasswordBox secure text for auth/change-password forms
			string password = txtPassword.Password;

			// Instantiate AuthService for login, logout, or password recovery
			AuthService authService = new AuthService();
			// Assign local/page state inside btnLogin_Click without altering business rules
			var user = authService.Login(username, password);
			// Guard clause: only continue when UI selection, role, or input is valid
			if (user != null)
			{
				// Create navigation window (login, dashboard, forgot password)
				var dashboard = new DashboardWindow(user);
				// Open window non-modally for navigation flow
				dashboard.Show();
				// Close current window after navigation or logout
				this.Close();
			}
			// Alternate branch when previous condition was not satisfied
			else
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Sai thông tin đăng nhập!");
			}
		}

		// Hyperlink quên mật khẩu (nếu XAML dùng Hyperlink thay vì TextBlock) — cùng luồng với lbForgotPassword_MouseDown
		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			// Create navigation window (login, dashboard, forgot password)
			var forgotPasswordWindow = new ForgotPasswordWindow();
			// Open window non-modally for navigation flow
			forgotPasswordWindow.Show();
			// Close current window after navigation or logout
			this.Close();
		}

		// Enter ở ô tài khoản — chuyển focus sang mật khẩu thay vì submit sớm
		private void txtUsername_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtPassword.Focus(); }
		}

		// Enter ở ô mật khẩu — kích hoạt đăng nhập như bấm nút
		private void txtPassword_KeyDown(object sender, KeyEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (e.Key == Key.Enter)
			{
				// Consume Enter key so WPF does not trigger default button twice
				e.Handled = true;
				// Delegate Enter key to the same handler as the primary save/login button
				btnLogin_Click(sender, e);
			}
		}
	}
}
