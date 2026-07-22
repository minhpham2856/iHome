using iHome.BLL.DTOs.Manager;
using iHome.BLL.Services.Manager;
using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace iHome.UI.Views.Manager
{
	// Trang danh mục dịch vụ và gán dịch vụ theo phòng
	public partial class ServicesPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _buildingId;
		private readonly ServiceCatalogService _service = new();
		private readonly RoomServiceAssignmentService _assignmentService = new();
		private List<ServiceDto> _services = new();
		private List<RoomServiceAssignmentDto> _assignments = new();
		private ICollectionView? _serviceView;
		private bool _isLoading;

		// Khởi tạo theo user và tòa nhà đang chọn
		public ServicesPage(User currentUser, int? buildingId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_buildingId = buildingId;
			Loaded += ServicesPage_Loaded;
		}

		// Nạp danh sách khi trang sẵn sàng
		private void ServicesPage_Loaded(object sender, RoutedEventArgs e) =>
			LoadServices();

		// Làm mới danh mục và gán phòng
		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadServices();

		// Tải dịch vụ + phân công phòng-dịch vụ
		private void LoadServices()
		{
			if (_isLoading)
			{
				return;
			}

			try
			{
				_isLoading = true;
				btnRefresh.IsEnabled = false;
				SetState("Đang tải danh sách dịch vụ...", true);
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				_services = _service.GetServices(managerId, _buildingId);
				_assignments = _assignmentService.GetAssignments(managerId, _buildingId);

				_serviceView = CollectionViewSource.GetDefaultView(_services);
				_serviceView.Filter = FilterService;
				dgServices.ItemsSource = _serviceView;
				dgAssignments.ItemsSource = _assignments;
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
				btnRefresh.IsEnabled = true;
			}
		}

		// Gán dịch vụ cho một phòng
		private void AssignService_Click(object sender, RoutedEventArgs e)
		{
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (roomsOk, rooms, roomsError) = ManagerUi.TryGet(() => _assignmentService.GetRoomOptions(managerId, _buildingId));
			var (servicesOk, services, servicesError) = ManagerUi.TryGet(() => _assignmentService.GetServiceOptions(managerId, _buildingId));
			if (!roomsOk || !servicesOk || rooms == null || services == null)
			{
				ManagerUi.ShowError(roomsError ?? servicesError ?? "Không thể tải dữ liệu.");
				return;
			}
			var dialog = new RoomServiceDialog(rooms, services) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true)
			{
				if (ManagerUi.TryRun(() => _assignmentService.SetAssignment(managerId, dialog.RoomId, dialog.ServiceId, true)))
				{
					LoadServices();
				}
			}
		}

		// Ngừng gán dịch vụ đang chọn
		private void UnassignService_Click(object sender, RoutedEventArgs e) =>
			SetSelectedAssignment(false);

		// Kích hoạt lại gán dịch vụ đang chọn
		private void ReactivateService_Click(object sender, RoutedEventArgs e) =>
			SetSelectedAssignment(true);

		// Bật/tắt trạng thái gán phòng-dịch vụ
		private void SetSelectedAssignment(bool isActive)
		{
			if (dgAssignments.SelectedItem is not RoomServiceAssignmentDto selected)
			{
				MessageBox.Show(
					"Vui lòng chọn dịch vụ trong tab 'Dịch vụ đã gán theo phòng'.",
					"Chưa chọn dữ liệu",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
				return;
			}
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			if (ManagerUi.TryRun(() => _assignmentService.SetAssignment(managerId, selected.RoomId, selected.ServiceId, isActive)))
			{
				LoadServices();
			}
		}

		// Nạp combo lọc tòa / trạng thái
		private void PopulateFilters()
		{
			cbBuilding.ItemsSource = new[] { All }
				.Concat(_services.Select(s => s.PropertyName).Distinct().OrderBy(name => name));
			cbStatus.ItemsSource = new[] { All }
				.Concat(_services.Select(s => s.StatusDisplay).Distinct().OrderBy(status => status));
			cbBuilding.SelectedIndex = 0;
			cbStatus.SelectedIndex = 0;
		}

		// Cập nhật KPI tổng / hoạt động / tắt / theo chỉ số
		private void UpdateSummary()
		{
			lbTotalServices.Text = _services.Count.ToString();
			lbActiveServices.Text = _services.Count(service => service.IsActive).ToString();
			lbInactiveServices.Text = _services.Count(service => !service.IsActive).ToString();
			lbMeteredServices.Text = _services.Count(service =>
				string.Equals(service.CalculationMethod, "Metered", StringComparison.OrdinalIgnoreCase)).ToString();
		}

		// Điều kiện lọc theo từ khóa và combo
		private bool FilterService(object item)
		{
			if (item is not ServiceDto service)
			{
				return false;
			}

			string keyword = txtSearch.Text.Trim();
			bool matchesKeyword = string.IsNullOrEmpty(keyword) ||
				service.ServiceName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				service.PropertyName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				service.Unit.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			bool matchesBuilding = cbBuilding.SelectedItem is not string building ||
				building == All || service.PropertyName == building;
			bool matchesStatus = cbStatus.SelectedItem is not string status ||
				status == All || service.StatusDisplay == status;

			return matchesKeyword && matchesBuilding && matchesStatus;
		}

		// Áp lại bộ lọc khi tìm kiếm / combo đổi
		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		// Làm mới view và đếm số dòng hiển thị
		private void RefreshView()
		{
			_serviceView?.Refresh();
			int count = _serviceView?.Cast<object>().Count() ?? 0;
			lbResultCount.Text = $"{count} dịch vụ";
			SetState(count == 0 ? "Không có dịch vụ phù hợp." : string.Empty, count == 0);
		}

		// Hiện / ẩn thông báo trạng thái trống
		private void SetState(string message, bool isVisible)
		{
			lbState.Text = message;
			brdState.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
