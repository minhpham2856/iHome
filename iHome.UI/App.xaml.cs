using System.Windows;

namespace iHome.UI
{
	// Entry point WPF — load biến môi trường (.env) trước khi mở LoginWindow
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			// Run WPF Application base startup before custom env loading
			base.OnStartup(e);
			// DotNetEnv: tìm file .env từ thư mục exe lên parent (SMTP, connection string…)
			DotNetEnv.Env.TraversePath().Load();
		}
	}
}
