using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using iHome.BLL.Services;

namespace iHome.UI
{
	/// <summary>
	/// Interaction logic for Login.xaml
	/// </summary>
	public partial class Login : Window
	{
		public Login()
		{
			InitializeComponent();
			txtUsername.Focus();
		}

		private void txtUsername_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				txtPassword.Focus();
			}
		}

		private void txtPassword_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				btnLogin_Click(sender, e);
			}
		}

		private void tbForgotPassword_MouseDown(object sender, MouseButtonEventArgs e)
		{

		}

		private void btnLogin_Click(object sender, RoutedEventArgs e)
		{
			string username = txtUsername.Text;
			string password = txtPassword.Password;

			AuthService authService = new AuthService();
			bool loginSuccess = authService.Login(username, password);
			if (loginSuccess)
			{
				MessageBox.Show("Login successful!");
			}
			else
			{
				MessageBox.Show("Không thể đăng nhập!");
			}
		}
	}
}
