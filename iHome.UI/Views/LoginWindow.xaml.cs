using System.Windows;
using System.Windows.Input;
using iHome.BLL.Services;

namespace iHome.UI.Views
{
	public partial class LoginWindow : Window
	{
		public LoginWindow()
		{
			InitializeComponent();
			txtUsername.Focus();
		}

		private void tbForgotPassword_MouseDown(object sender, MouseButtonEventArgs e)
		{
			new ForgotPasswordWindow().Show();
			Close();
		}

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

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			var forgotPasswordWindow = new ForgotPasswordWindow();
			forgotPasswordWindow.Show();
			this.Close();
		}

		// key down enter
		private void txtUsername_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtPassword.Focus(); }
		}

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
