using System;
using System.Windows;
using System.Windows.Controls;

namespace iHome.UI.Views
{
	public partial class DashboardWindow : Window
	{
		public string CurrentRole { get; private set; }

		public DashboardWindow(string role)
		{
			InitializeComponent();
			CurrentRole = role;
			LoadSidebar();
		}

		private void LoadSidebar()
		{
			var menuItems = new System.Collections.Generic.List<SidebarItem>();

			if (CurrentRole == "Landlord")
			{
				menuItems.Add(new SidebarItem { Title = "Bảng điều khiển", PageName = "DashboardPage" });
				menuItems.Add(new SidebarItem { Title = "Quản lý nhà trọ", PageName = "BuildingsPage" });
				menuItems.Add(new SidebarItem { Title = "Quản lý phòng", PageName = "RoomsPage" });
				menuItems.Add(new SidebarItem { Title = "Quản lý khách", PageName = "GuestsPage" });
				menuItems.Add(new SidebarItem { Title = "Quản lý hợp đồng", PageName = "ContractsPage" });
				menuItems.Add(new SidebarItem { Title = "Quản lý nhân viên", PageName = "ManagersPage" });
				menuItems.Add(new SidebarItem { Title = "Quản lý dịch vụ", PageName = "ServicesPage" });
				menuItems.Add(new SidebarItem { Title = "Báo cáo thống kê", PageName = "ReportsPage" });
				menuItems.Add(new SidebarItem { Title = "Cài đặt", PageName = "SettingsPage" });
			}
			else if (CurrentRole == "Manager")
			{
				menuItems.Add(new SidebarItem { Title = "Bảng điều khiển", PageName = "DashboardPage" });
				menuItems.Add(new SidebarItem { Title = "Quản lý phòng", PageName = "RoomsPage" });
				menuItems.Add(new SidebarItem { Title = "Quản lý khách", PageName = "GuestsPage" });
				menuItems.Add(new SidebarItem { Title = "Quản lý dịch vụ", PageName = "ServicesPage" });
			}

			icSidebarMenu.ItemsSource = menuItems;
		}

		private void Forward(string pageName, string pageTitle = "")
		{
			string uriString = $"Views/{CurrentRole}/{pageName}.xaml";
			mainFrame.Navigate(new Uri(uriString, UriKind.Relative));
			if (!string.IsNullOrEmpty(pageTitle))
			{
				txtPageTitle.Text = pageTitle;
			}
		}

		private void SidebarButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is SidebarItem item)
			{
				Forward(item.PageName, item.Title);
			}
		}

		private void btnLogout_Click(object sender, RoutedEventArgs e)
		{
			new LoginWindow().Show();
			this.Close();
		}
	}

	public class SidebarItem
	{
		public string Title { get; set; }
		public string PageName { get; set; }
	}
}
