using iHome.BLL.DTOs;
using iHome.BLL.Services.Manager;
using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace iHome.UI.Views.Manager
{
	public partial class ServicesPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _buildingId;
		private readonly ManagerServiceCatalogService _service = new();
		private readonly ManagerRoomServiceAssignmentService _assignmentService = new();
		private List<ManagerServiceDto> _services = new();
		private List<ManagerRoomServiceAssignmentDto> _assignments = new();
		private ICollectionView? _serviceView;
		private bool _isLoading;

		public ServicesPage(User currentUser, int? buildingId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_buildingId = buildingId;
			Loaded += ServicesPage_Loaded;
		}

		private async void ServicesPage_Loaded(object sender, RoutedEventArgs e) =>
			await LoadServicesAsync();

		private async void BtnRefresh_Click(object sender, RoutedEventArgs e) =>
			await LoadServicesAsync();

		private async Task LoadServicesAsync()
		{
			if (_isLoading)
			{
				return;
			}

			try
			{
				_isLoading = true;
				BtnRefresh.IsEnabled = false;
				SetState("Đang tải danh sách dịch vụ...", true);
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				_services = await Task.Run(() => _service.GetServices(managerId, _buildingId));
				_assignments = await Task.Run(() => _assignmentService.GetAssignments(managerId, _buildingId));

				_serviceView = CollectionViewSource.GetDefaultView(_services);
				_serviceView.Filter = FilterService;
				ServicesGrid.ItemsSource = _serviceView;
				AssignmentsGrid.ItemsSource = _assignments;
				PopulateFilters();
				UpdateSummary();
				RefreshView();
			}
			catch (Exception)
			{
				SetState("Không thể tải danh sách dịch vụ. Vui lòng thử lại.", true);
			}
			finally
			{
				_isLoading = false;
				BtnRefresh.IsEnabled = true;
			}
		}

		private async void AssignService_Click(object sender, RoutedEventArgs e)
		{
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (roomsOk, rooms, roomsError) = await ManagerUi.TryGetAsync(() => _assignmentService.GetRoomOptions(managerId, _buildingId));
			var (servicesOk, services, servicesError) = await ManagerUi.TryGetAsync(() => _assignmentService.GetServiceOptions(managerId, _buildingId));
			if (!roomsOk || !servicesOk || rooms == null || services == null)
			{
				ManagerUi.ShowError(roomsError ?? servicesError ?? "Không thể tải dữ liệu.");
				return;
			}
			var dialog = new RoomServiceDialog(rooms, services) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true)
			{
				if (await ManagerUi.TryRunAsync(() => _assignmentService.SetAssignment(managerId, dialog.RoomId, dialog.ServiceId, true)))
				{
					await LoadServicesAsync();
				}
			}
		}

		private async void UnassignService_Click(object sender, RoutedEventArgs e) =>
			await SetSelectedAssignmentAsync(false);

		private async void ReactivateService_Click(object sender, RoutedEventArgs e) =>
			await SetSelectedAssignmentAsync(true);

		private async Task SetSelectedAssignmentAsync(bool isActive)
		{
			if (AssignmentsGrid.SelectedItem is not ManagerRoomServiceAssignmentDto selected)
			{
				MessageBox.Show(
					"Vui lòng chọn dịch vụ trong tab 'Dịch vụ đã gán theo phòng'.",
					"Chưa chọn dữ liệu",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
				return;
			}
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			if (await ManagerUi.TryRunAsync(() => _assignmentService.SetAssignment(managerId, selected.RoomId, selected.ServiceId, isActive)))
			{
				await LoadServicesAsync();
			}
		}

		private void PopulateFilters()
		{
			CbBuilding.ItemsSource = new[] { All }
				.Concat(_services.Select(s => s.PropertyName).Distinct().OrderBy(name => name));
			CbStatus.ItemsSource = new[] { All }
				.Concat(_services.Select(s => s.StatusDisplay).Distinct().OrderBy(status => status));
			CbBuilding.SelectedIndex = 0;
			CbStatus.SelectedIndex = 0;
		}

		private void UpdateSummary()
		{
			TxtTotalServices.Text = _services.Count.ToString();
			TxtActiveServices.Text = _services.Count(service => service.IsActive).ToString();
			TxtInactiveServices.Text = _services.Count(service => !service.IsActive).ToString();
			TxtMeteredServices.Text = _services.Count(service =>
				string.Equals(service.CalculationMethod, "Metered", StringComparison.OrdinalIgnoreCase)).ToString();
		}

		private bool FilterService(object item)
		{
			if (item is not ManagerServiceDto service)
			{
				return false;
			}

			string keyword = TxtSearch.Text.Trim();
			bool matchesKeyword = string.IsNullOrEmpty(keyword) ||
				service.ServiceName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				service.PropertyName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				service.Unit.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			bool matchesBuilding = CbBuilding.SelectedItem is not string building ||
				building == All || service.PropertyName == building;
			bool matchesStatus = CbStatus.SelectedItem is not string status ||
				status == All || service.StatusDisplay == status;

			return matchesKeyword && matchesBuilding && matchesStatus;
		}

		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		private void RefreshView()
		{
			_serviceView?.Refresh();
			int count = _serviceView?.Cast<object>().Count() ?? 0;
			TxtResultCount.Text = $"{count} dịch vụ";
			SetState(count == 0 ? "Không có dịch vụ phù hợp." : string.Empty, count == 0);
		}

		private void SetState(string message, bool isVisible)
		{
			StateText.Text = message;
			StatePanel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
