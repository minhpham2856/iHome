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

namespace iHome.UI.Views
{

	public partial class ForgotPasswordWindow : Window
	{
		public ForgotPasswordWindow()
		{
			InitializeComponent();
		}

		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				RecoverPassword();
			}
		}

		private void tbReturnToLogin_MouseDown(object sender, MouseButtonEventArgs e)
		{
			new LoginWindow().Show();
			this.Close();
		}

		private void btnLogin_Click(object sender, RoutedEventArgs e)
		{
			RecoverPassword();
		}

		private void RecoverPassword()
		{

		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			var loginWindow = new LoginWindow();
			loginWindow.Show();
			this.Close();
		}
	}
}
