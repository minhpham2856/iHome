using System.Windows;
using System.Windows.Input;
using iHome.BLL.Services;

namespace iHome.UI.Views
{
	// Login entry — AuthService then DashboardWindow
	public partial class LoginWindow : Window
	{
		public LoginWindow()
		{
			InitializeComponent();
			txtUsername.Focus();
		}

		// Open forgot-password flow
		private void lbForgotPassword_MouseDown(object sender, MouseButtonEventArgs e)
		{
			new ForgotPasswordWindow().Show();
			Close();
		}

		// Validate credentials and open the main shell
		private void btnLogin_Click(object sender, RoutedEventArgs e)
		{
			string username = txtUsername.Text;
			string password = txtPassword.Password;

			AuthService authService = new AuthService();
			var user = authService.Login(username, password);
			if (user != null)
			{
				var dashboard = new DashboardWindow(user);
				dashboard.Show();
				this.Close();
			}
			else
			{
				MessageBox.Show("Sai thông tin đăng nhập!");
			}
		}

		// Same navigation as lbForgotPassword_MouseDown (Hyperlink variant)
		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			var forgotPasswordWindow = new ForgotPasswordWindow();
			forgotPasswordWindow.Show();
			this.Close();
		}

		// Enter in username moves focus to password
		private void txtUsername_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtPassword.Focus(); }
		}

		// Enter in password submits login
		private void txtPassword_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				btnLogin_Click(sender, e);
			}
		}
	}
}
