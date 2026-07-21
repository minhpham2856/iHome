using iHome.BLL.DTOs;
using iHome.BLL.Services;
using iHome.BLL.Services.Manager;
using iHome.DAL.Entities;
using iHome.UI.Views.Shared;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace iHome.UI.Views
{
	public partial class DashboardWindow : Window
	{
		private readonly User _currentUser;
		private readonly ManagerBuildingService _managerBuildingService = new();
		private bool _isLoadingBuildingFilter;
		private string _currentPageName = "DashboardPage";
		private string _currentPageTitle = "Bảng điều khiển";
		private int? _selectedBuildingId;

		public string CurrentRole => _currentUser.Role;

		public void RefreshWelcome()
		{
			txtWelcome.Text = $"Xin chào, {_currentUser.FullName}";
		}

		public DashboardWindow(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			MainSidebar.MenuItemSelected += MainSidebar_MenuItemSelected;
			MainSidebar.LogoutRequested += MainSidebar_LogoutRequested;

			RefreshWelcome();
			LoadSidebar();
			LoadBuildingFilter();
			Forward(_currentPageName, _currentPageTitle);
		}

		private void LoadSidebar()
		{
			var menuItems = new List<SidebarMenuItem>();

			if (CurrentRole == "Landlord")
			{
				menuItems.Add(new SidebarMenuItem { Title = "Bảng điều khiển", PageName = "DashboardPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý nhà trọ", PageName = "BuildingsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý phòng", PageName = "RoomsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý khách", PageName = "GuestsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý hợp đồng", PageName = "ContractsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý nhân viên", PageName = "ManagersPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý dịch vụ", PageName = "ServicesPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Báo cáo thống kê", PageName = "ReportsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Nhật ký", PageName = "AuditLogsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Cài đặt", PageName = "SettingsPage" });
			}
			else if (CurrentRole == "Manager")
			{
				menuItems.Add(new SidebarMenuItem { Title = "Bảng điều khiển", PageName = "DashboardPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý phòng", PageName = "RoomsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý khách", PageName = "GuestsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý hợp đồng", PageName = "ContractsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý hóa đơn", PageName = "InvoicesPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý dịch vụ", PageName = "ServicesPage" });
				// Cài đặt tài khoản Manager (hồ sơ + đổi mật khẩu)
				menuItems.Add(new SidebarMenuItem { Title = "Cài đặt tài khoản", PageName = "SettingsPage" });
			}
			else
			{
				MessageBox.Show("Vai trò người dùng không hợp lệ.");
				Close();
				return;
			}

			MainSidebar.SetItems(menuItems);
		}

		private void LoadBuildingFilter()
		{
			if (CurrentRole != "Manager")
			{
				CbProperty.Visibility = Visibility.Collapsed;
				return;
			}

			try
			{
				_isLoadingBuildingFilter = true;
				var buildings = _managerBuildingService.GetAssignedBuildings(_currentUser.Id);
				buildings.Insert(0, new ManagerBuildingOptionDto
				{
					Id = null,
					Name = "Tất cả tòa"
				});

				CbProperty.ItemsSource = buildings;
				CbProperty.SelectedIndex = 0;
				CbProperty.Visibility = Visibility.Visible;
			}
			catch (Exception)
			{
				CbProperty.Visibility = Visibility.Collapsed;
				MessageBox.Show("Không thể tải danh sách tòa nhà được phân công.");
			}
			finally
			{
				_isLoadingBuildingFilter = false;
			}
		}

		private void Forward(string pageName, string pageTitle = "")
		{
			_currentPageName = pageName;
			_currentPageTitle = string.IsNullOrWhiteSpace(pageTitle)
				? _currentPageTitle
				: pageTitle;

			if (CurrentRole == "Manager")
			{
				mainFrame.Navigate(CreateManagerPage(pageName));
			}
			else if (CurrentRole == "Landlord")
			{
				mainFrame.Navigate(CreateLandlordPage(pageName));
			}
			else
			{
				string uriString = $"Views/{CurrentRole}/{pageName}.xaml";
				mainFrame.Navigate(new Uri(uriString, UriKind.Relative));
			}

			txtPageTitle.Text = _currentPageTitle;
			MainSidebar.SetSelected(pageName);
		}

		private Page CreateLandlordPage(string pageName) => pageName switch
		{
			"DashboardPage" => new Landlord.DashboardPage(_currentUser),
			"BuildingsPage" => new Landlord.BuildingsPage(_currentUser),
			"RoomsPage" => new Landlord.RoomsPage(_currentUser),
			"GuestsPage" => new Landlord.GuestsPage(_currentUser),
			"ContractsPage" => new Landlord.ContractsPage(),
			"ManagersPage" => new Landlord.ManagersPage(_currentUser),
			"ServicesPage" => new Landlord.ServicesPage(_currentUser),
			"AuditLogsPage" => new Landlord.AuditLogsPage(_currentUser),
			"ReportsPage" => new Landlord.ReportsPage(),
			"SettingsPage" => new Landlord.SettingsPage(_currentUser),
			_ => throw new InvalidOperationException("Trang chủ trọ không tồn tại.")
		};

		private Page CreateManagerPage(string pageName) => pageName switch
		{
			"DashboardPage" => new Manager.DashboardPage(_currentUser, _selectedBuildingId),
			"RoomsPage" => new Manager.RoomsPage(_currentUser, _selectedBuildingId),
			"GuestsPage" => new Manager.GuestsPage(_currentUser, _selectedBuildingId),
			"ContractsPage" => new Manager.ContractsPage(_currentUser, _selectedBuildingId),
			"InvoicesPage" => new Manager.InvoicesPage(_currentUser, _selectedBuildingId),
			"ServicesPage" => new Manager.ServicesPage(_currentUser, _selectedBuildingId),
			"SettingsPage" => new Manager.SettingsPage(_currentUser),
			_ => throw new InvalidOperationException("Trang quản lý không tồn tại.")
		};

		private void MainSidebar_MenuItemSelected(SidebarMenuItem item) =>
			Forward(item.PageName, item.Title);

		private void CbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isLoadingBuildingFilter || CurrentRole != "Manager")
			{
				return;
			}

			_selectedBuildingId = (CbProperty.SelectedItem as ManagerBuildingOptionDto)?.Id;
			Forward(_currentPageName, _currentPageTitle);
		}

		private void MainSidebar_LogoutRequested()
		{
			try
			{
				new AuthService().Logout(_currentUser.Id);
			}
			catch
			{
			}

			new LoginWindow().Show();
			Close();
		}

		// stretch page content to fill the frame (window - sidebar - header)
		private void mainFrame_Navigated(object sender, NavigationEventArgs e)
		{
			if (mainFrame.Content is FrameworkElement page)
			{
				page.HorizontalAlignment = HorizontalAlignment.Stretch;
				page.VerticalAlignment = VerticalAlignment.Stretch;
				page.Width = double.NaN;
				page.Height = double.NaN;
			}
		}
	}
}
