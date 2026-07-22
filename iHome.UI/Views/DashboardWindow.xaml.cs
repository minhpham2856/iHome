using iHome.BLL.DTOs.Manager;
using iHome.BLL.Services;
using iHome.BLL.Services.Manager;
using iHome.BLL.Enums;
using iHome.DAL.Entities;
using iHome.UI.Views.Shared;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace iHome.UI.Views
{
	// Main shell after login — role menu, optional building filter, Frame navigation
	public partial class DashboardWindow : Window
	{
		private readonly User _currentUser;
		private readonly BuildingService _managerBuildingService = new();
		private bool _isLoadingBuildingFilter;
		private string _currentPageName = "DashboardPage";
		private string _currentPageTitle = "Bảng điều khiển";
		private int? _selectedBuildingId;
		public string CurrentRole => _currentUser.Role;

		public void RefreshWelcome()
		{
			lbWelcome.Text = $"Xin chào, {_currentUser.FullName}";
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

		// Build role-specific sidebar entries
		private void LoadSidebar()
		{
			var menuItems = new List<SidebarMenuItem>();
			if (CurrentRole == UserRole.Landlord)
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
			else if (CurrentRole == UserRole.Manager)
			{
				menuItems.Add(new SidebarMenuItem { Title = "Bảng điều khiển", PageName = "DashboardPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý phòng", PageName = "RoomsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý khách", PageName = "GuestsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý hợp đồng", PageName = "ContractsPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý hóa đơn", PageName = "InvoicesPage" });
				menuItems.Add(new SidebarMenuItem { Title = "Quản lý dịch vụ", PageName = "ServicesPage" });
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

		// Manager-only building combo; null Id = all buildings
		private void LoadBuildingFilter()
		{
			if (CurrentRole != UserRole.Manager)
			{
				cbProperty.Visibility = Visibility.Collapsed;
				return;
			}
			try
			{
				_isLoadingBuildingFilter = true;
				var buildings = _managerBuildingService.GetAssignedBuildings(_currentUser.Id);
				buildings.Insert(0, new BuildingOptionDto
				{
					Id = null,
					Name = "Tất cả tòa"
				});
				cbProperty.ItemsSource = buildings;
				cbProperty.SelectedIndex = 0;
				cbProperty.Visibility = Visibility.Visible;
			}
			catch (Exception)
			{
				cbProperty.Visibility = Visibility.Collapsed;
				MessageBox.Show("Không thể tải danh sách tòa nhà được phân công.");
			}
			finally
			{
				_isLoadingBuildingFilter = false;
			}
		}

		// Navigate Frame to the page for the current role
		private void Forward(string pageName, string pageTitle = "")
		{
			_currentPageName = pageName;
			_currentPageTitle = string.IsNullOrWhiteSpace(pageTitle)
				? _currentPageTitle
				: pageTitle;
			if (CurrentRole == UserRole.Manager)
			{
				mainFrame.Navigate(CreateManagerPage(pageName));
			}
			else if (CurrentRole == UserRole.Landlord)
			{
				mainFrame.Navigate(CreateLandlordPage(pageName));
			}
			else
			{
				string uriString = $"Views/{CurrentRole}/{pageName}.xaml";
				mainFrame.Navigate(new Uri(uriString, UriKind.Relative));
			}
			lbPageTitle.Text = _currentPageTitle;
			MainSidebar.SetSelected(pageName);
		}

		private Page CreateLandlordPage(string pageName) => pageName switch
		{
			"DashboardPage" => new Landlord.DashboardPage(_currentUser),
			"BuildingsPage" => new Landlord.BuildingsPage(_currentUser),
			"RoomsPage" => new Landlord.RoomsPage(_currentUser),
			"GuestsPage" => new Landlord.GuestsPage(_currentUser),
			"ContractsPage" => new Landlord.ContractsPage(_currentUser),
			"ManagersPage" => new Landlord.ManagersPage(_currentUser),
			"ServicesPage" => new Landlord.ServicesPage(_currentUser),
			"AuditLogsPage" => new Landlord.AuditLogsPage(_currentUser),
			"ReportsPage" => new Landlord.ReportsPage(_currentUser),
			"SettingsPage" => new Landlord.SettingsPage(_currentUser),
			_ => throw new InvalidOperationException("Trang chủ trọ không tồn tại.")
		};

		// Pass selected building into manager pages for scoping
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

		// Reload current page when building filter changes
		private void cbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isLoadingBuildingFilter || CurrentRole != UserRole.Manager)
			{
				return;
			}
			_selectedBuildingId = (cbProperty.SelectedItem as BuildingOptionDto)?.Id;
			Forward(_currentPageName, _currentPageTitle);
		}

		private void MainSidebar_LogoutRequested()
		{
			try
			{
				new AuthService().Logout(_currentUser);
			}
			catch { }
			new LoginWindow().Show();
			Close();
		}

		// Stretch Frame content to fill the content area
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
