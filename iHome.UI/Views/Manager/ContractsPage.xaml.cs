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
	// Trang hợp đồng: lọc, CRUD và gắn khách vào hợp đồng
	public partial class ContractsPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _buildingId;
		private readonly ContractService _service = new();
		private readonly TenantService _tenantService = new();
		private List<ContractDto> _contracts = new();
		private ICollectionView? _view;
		private bool _isLoading;

		// Khởi tạo theo user và tòa nhà đang chọn
		public ContractsPage(User currentUser, int? buildingId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_buildingId = buildingId;
			Loaded += (_, _) => Load();
		}

		// Làm mới danh sách hợp đồng
		private void btnRefresh_Click(object sender, RoutedEventArgs e) => Load();

		// Tải hợp đồng và gắn bộ lọc
		private void Load()
		{
			if (_isLoading) return;
			try
			{
				_isLoading = true;
				btnRefresh.IsEnabled = false;
				SetState("Đang tải danh sách hợp đồng...", true);
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				var (ok, contracts, error) = ManagerUi.TryGet(() => _service.GetContracts(managerId, _buildingId));
				if (!ok || contracts == null)
				{
					SetState(error ?? "Không thể tải danh sách hợp đồng.", true);
					return;
				}
				_contracts = contracts;
				_view = CollectionViewSource.GetDefaultView(_contracts);
				_view.Filter = FilterContract;
				dgContracts.ItemsSource = _view;
				cbStatus.ItemsSource = new[] { All }.Concat(_contracts.Select(item => item.StatusDisplay).Distinct());
				cbStatus.SelectedIndex = 0;
				UpdateSummary();
				RefreshView();
			}
			finally { _isLoading = false; btnRefresh.IsEnabled = true; }
		}

		// Cập nhật KPI; sắp hết hạn theo StatusDisplay thời gian thực
		private void UpdateSummary()
		{
			lbTotal.Text = _contracts.Count.ToString();
			lbActive.Text = _contracts.Count(item => item.Status == "Active").ToString();
			// Đếm theo StatusDisplay đã tính thời gian thực (còn < 1 tháng → Sắp hết hạn)
			lbExpiring.Text = _contracts.Count(item => item.StatusDisplay == "Sắp hết hạn").ToString();
			lbTerminated.Text = _contracts.Count(item => item.Status == "Terminated").ToString();
		}

		// Điều kiện lọc theo từ khóa và trạng thái
		private bool FilterContract(object item)
		{
			if (item is not ContractDto contract) return false;
			string keyword = txtSearch.Text.Trim();
			bool search = string.IsNullOrEmpty(keyword)
				|| contract.Id.ToString().Contains(keyword)
				|| contract.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
				|| contract.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
				|| contract.MainTenantName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
				|| contract.TenantNames.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			bool status = cbStatus.SelectedItem is not string selected || selected == All || selected == contract.StatusDisplay;
			return search && status;
		}

		// Áp lại bộ lọc khi tìm kiếm / combo đổi
		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		// Làm mới view và đếm số dòng
		private void RefreshView() { _view?.Refresh(); int count = _view?.Cast<object>().Count() ?? 0; lbResultCount.Text = $"{count} hợp đồng"; SetState(count == 0 ? "Không có hợp đồng phù hợp." : string.Empty, count == 0); }

		// Tạo hợp đồng mới
		private void AddContract_Click(object sender, RoutedEventArgs e)
		{
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (roomsOk, rooms, roomsError) = ManagerUi.TryGet(() => _service.GetRoomOptions(managerId, _buildingId));
			var (tenantsOk, tenants, tenantsError) = ManagerUi.TryGet(() => _tenantService.GetTenantOptions(managerId));
			if (!roomsOk || !tenantsOk || rooms == null || tenants == null)
			{
				ManagerUi.ShowError(roomsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				return;
			}
			var dialog = new ContractDialog(rooms, tenants) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				if (ManagerUi.TryRun(() => { _service.CreateContract(managerId, dialog.Result); }))
				{
					Load();
				}
			}
		}

		// Sửa hợp đồng đang chọn
		private void EditContract_Click(object sender, RoutedEventArgs e)
		{
			if (dgContracts.SelectedItem is not ContractDto selected) { ShowSelect(); return; }
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (formOk, form, formError) = ManagerUi.TryGet(() => _service.GetContract(managerId, selected.Id));
			var (roomsOk, rooms, roomsError) = ManagerUi.TryGet(() => _service.GetRoomOptions(managerId, _buildingId));
			var (tenantsOk, tenants, tenantsError) = ManagerUi.TryGet(() => _tenantService.GetTenantOptions(managerId));
			if (!formOk || !roomsOk || !tenantsOk || form == null || rooms == null || tenants == null)
			{
				ManagerUi.ShowError(formError ?? roomsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				return;
			}
			var dialog = new ContractDialog(rooms, tenants, form) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				if (ManagerUi.TryRun(() => _service.UpdateContract(managerId, dialog.Result)))
				{
					Load();
				}
			}
		}

		// Gắn khách vào hợp đồng (có thể chọn sẵn hợp đồng trên lưới)
		private void AssignTenant_Click(object sender, RoutedEventArgs e)
		{
			int? selectedContractId = (dgContracts.SelectedItem as ContractDto)?.Id;
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (contractsOk, contracts, contractsError) = ManagerUi.TryGet(() => _service.GetContractOptions(managerId, _buildingId));
			var (tenantsOk, tenants, tenantsError) = ManagerUi.TryGet(() => _tenantService.GetTenantOptions(managerId));
			if (!contractsOk || !tenantsOk || contracts == null || tenants == null)
			{
				ManagerUi.ShowError(contractsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				return;
			}
			var dialog = new AssignTenantDialog(contracts, tenants, null, selectedContractId) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true)
			{
				if (ManagerUi.TryRun(() => _service.AssignTenant(managerId, dialog.ContractId, dialog.TenantId, dialog.IsMainTenant)))
				{
					Load();
				}
			}
		}

		// Xóa hợp đồng sau khi xác nhận
		private void DeleteContract_Click(object sender, RoutedEventArgs e)
		{
			if (dgContracts.SelectedItem is not ContractDto selected) { ShowSelect(); return; }
			if (MessageBox.Show($"Xóa hợp đồng #{selected.Id}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			if (ManagerUi.TryRun(() => _service.DeleteContract(managerId, selected.Id)))
			{
				Load();
			}
		}

		// Hiện / ẩn thông báo trạng thái
		private void SetState(string message, bool visible) { lbState.Text = message; brdState.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; }

		// Nhắc chọn hợp đồng trước khi thao tác
		private static void ShowSelect() => MessageBox.Show("Vui lòng chọn hợp đồng.", "Chưa chọn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}
