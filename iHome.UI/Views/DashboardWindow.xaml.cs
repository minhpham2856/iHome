using iHome.BLL.DTOs;
using iHome.BLL.Services.Manager;
using iHome.DAL.Entities;
using iHome.UI.Views.Shared;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace iHome.UI.Views
{
	public partial class DashboardWindow : Window
	{
		private readonly User _currentUser;
		private readonly ManagerBuildingService _managerBuildingService = new();
		private bool _isLoadingPropertyFilter;
		private string _currentPageName = "DashboardPage";
		private string _currentPageTitle = "Bảng điều khiển";
		private int? _selectedPropertyId;

		public string CurrentRole => _currentUser.Role;

		public DashboardWindow(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			MainSidebar.MenuItemSelected += MainSidebar_MenuItemSelected;
			MainSidebar.LogoutRequested += MainSidebar_LogoutRequested;

			txtWelcome.Text = $"Xin chào, {_currentUser.FullName}";
			LoadSidebar();
			LoadPropertyFilter();
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
			}
			else
			{
				MessageBox.Show("Vai trò người dùng không hợp lệ.");
				Close();
				return;
			}

			MainSidebar.SetItems(menuItems);
		}

		private void LoadPropertyFilter()
		{
			if (CurrentRole != "Manager")
			{
				CboProperty.Visibility = Visibility.Collapsed;
				return;
			}

			try
			{
				_isLoadingPropertyFilter = true;
				var properties = _managerBuildingService.GetProperties(_currentUser.Id);
				properties.Insert(0, new ManagerPropertyOptionDto
				{
					Id = null,
					Name = "Tất cả nhà trọ"
				});

				CboProperty.ItemsSource = properties;
				CboProperty.SelectedIndex = 0;
				CboProperty.Visibility = Visibility.Visible;
			}
			catch (Exception)
			{
				CboProperty.Visibility = Visibility.Collapsed;
				MessageBox.Show("Không thể tải danh sách nhà trọ được phân công.");
			}
			finally
			{
				_isLoadingPropertyFilter = false;
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
			else
			{
				string uriString = $"Views/{CurrentRole}/{pageName}.xaml";
				mainFrame.Navigate(new Uri(uriString, UriKind.Relative));
			}

			txtPageTitle.Text = _currentPageTitle;
			MainSidebar.SetSelected(pageName);
		}

		private Page CreateManagerPage(string pageName) => pageName switch
		{
			"DashboardPage" => new Manager.DashboardPage(_currentUser, _selectedPropertyId),
			"RoomsPage" => new Manager.RoomsPage(_currentUser, _selectedPropertyId),
			"GuestsPage" => new Manager.GuestsPage(_currentUser, _selectedPropertyId),
			"ContractsPage" => new Manager.ContractsPage(_currentUser, _selectedPropertyId),
			"InvoicesPage" => new Manager.InvoicesPage(_currentUser, _selectedPropertyId),
			"ServicesPage" => new Manager.ServicesPage(_currentUser, _selectedPropertyId),
			_ => throw new InvalidOperationException("Trang quản lý không tồn tại.")
		};

		private void MainSidebar_MenuItemSelected(SidebarMenuItem item) =>
			Forward(item.PageName, item.Title);

		private void CboProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isLoadingPropertyFilter || CurrentRole != "Manager")
			{
				return;
			}

			_selectedPropertyId = (CboProperty.SelectedItem as ManagerPropertyOptionDto)?.Id;
			Forward(_currentPageName, _currentPageTitle);
		}

		private void MainSidebar_LogoutRequested()
		{
			new LoginWindow().Show();
			Close();
		}
	}
}
