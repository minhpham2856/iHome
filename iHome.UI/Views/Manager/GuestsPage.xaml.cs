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
	// Trang danh sách khách thuê: lọc, thêm/sửa/xóa, gắn/gỡ khỏi hợp đồng
	public partial class GuestsPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _buildingId;
		private readonly TenantService _service = new();
		private readonly ContractService _contractService = new();
		private List<TenantDto> _tenants = new();
		private ICollectionView? _tenantView;
		private bool _isLoading;

		// Khởi tạo trang theo user và tòa nhà đang chọn
		public GuestsPage(User currentUser, int? buildingId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_buildingId = buildingId;
			Loaded += GuestsPage_Loaded;
		}

		// Nạp danh sách khi trang sẵn sàng
		private void GuestsPage_Loaded(object sender, RoutedEventArgs e) =>
			LoadTenants();

		// Làm mới danh sách khách
		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadTenants();

		// Thêm khách mới; chỉ gắn phòng khi tạo hợp đồng
		private void AddTenant_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new TenantDialog { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() != true || dialog.Result == null)
			{
				return;
			}

			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (ok, _) = ManagerUi.TryRun(() => _service.CreateTenant(managerId, dialog.Result));
			if (!ok)
			{
				return;
			}
			LoadTenants();
			// Không gán phòng ngay: khách chỉ vào phòng khi tạo hợp đồng
			MessageBox.Show(
				"Đã thêm khách. Hãy tạo hợp đồng để gắn khách vào phòng.",
				"Thêm khách thành công",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		// Sửa thông tin khách đang chọn
		private void EditTenant_Click(object sender, RoutedEventArgs e)
		{
			if (dgGuests.SelectedItem is not TenantDto selected)
			{
				ShowSelectMessage("Vui lòng chọn khách cần sửa.");
				return;
			}

			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (formOk, form, formError) = ManagerUi.TryGet(() => _service.GetTenant(managerId, selected.TenantId));
			if (!formOk || form == null)
			{
				ManagerUi.ShowError(formError ?? "Không thể tải thông tin khách.");
				return;
			}
			var dialog = new TenantDialog(form) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				if (ManagerUi.TryRun(() => _service.UpdateTenant(managerId, dialog.Result)))
				{
					LoadTenants();
				}
			}
		}

		// Xóa khách đang chọn sau khi xác nhận
		private void DeleteTenant_Click(object sender, RoutedEventArgs e)
		{
			if (dgGuests.SelectedItem is not TenantDto selected)
			{
				ShowSelectMessage("Vui lòng chọn khách cần xóa.");
				return;
			}
			if (MessageBox.Show(
				$"Xóa khách {selected.FullName}?",
				"Xác nhận xóa",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning) != MessageBoxResult.Yes)
			{
				return;
			}

			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			if (ManagerUi.TryRun(() => _service.DeleteTenant(managerId, selected.TenantId)))
			{
				LoadTenants();
			}
		}

		// Mở dialog gắn khách vào hợp đồng
		private void AssignTenant_Click(object sender, RoutedEventArgs e)
		{
			int? tenantId = (dgGuests.SelectedItem as TenantDto)?.TenantId;
			ShowAssignDialog(tenantId);
		}

		// Gắn khách vào hợp đồng (có thể chọn sẵn khách)
		private void ShowAssignDialog(int? selectedTenantId)
		{
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (contractsOk, contracts, contractsError) = ManagerUi.TryGet(() => _contractService.GetContractOptions(managerId, _buildingId));
			var (tenantsOk, tenants, tenantsError) = ManagerUi.TryGet(() => _service.GetTenantOptions(managerId));
			if (!contractsOk || !tenantsOk || contracts == null || tenants == null)
			{
				ManagerUi.ShowError(contractsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				return;
			}
			var dialog = new AssignTenantDialog(contracts, tenants, selectedTenantId)
			{
				Owner = Window.GetWindow(this)
			};
			if (dialog.ShowDialog() == true)
			{
				if (ManagerUi.TryRun(() => _contractService.AssignTenant(
					managerId,
					dialog.ContractId,
					dialog.TenantId,
					dialog.IsMainTenant)))
				{
					LoadTenants();
				}
			}
		}

		// Gỡ khách khỏi hợp đồng hiện tại
		private void RemoveTenant_Click(object sender, RoutedEventArgs e)
		{
			if (dgGuests.SelectedItem is not TenantDto selected || selected.ContractId <= 0)
			{
				ShowSelectMessage("Vui lòng chọn một khách đang thuê thuộc hợp đồng.");
				return;
			}
			if (MessageBox.Show(
				$"Gỡ {selected.FullName} khỏi hợp đồng #{selected.ContractId}?",
				"Xác nhận gỡ khách",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question) != MessageBoxResult.Yes)
			{
				return;
			}

			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			if (ManagerUi.TryRun(() => _contractService.RemoveTenant(managerId, selected.ContractId, selected.TenantId)))
			{
				LoadTenants();
			}
		}

		// Tải danh sách khách và gắn bộ lọc
		private void LoadTenants()
		{
			if (_isLoading)
			{
				return;
			}

			try
			{
				_isLoading = true;
				btnRefresh.IsEnabled = false;
				SetState("Đang tải danh sách người thuê...", true);
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				_tenants = _service.GetTenants(managerId, _buildingId);

				_tenantView = CollectionViewSource.GetDefaultView(_tenants);
				_tenantView.Filter = FilterTenant;
				dgGuests.ItemsSource = _tenantView;
				PopulateFilters();
				UpdateSummary();
				RefreshView();
			}
			catch (Exception)
			{
				SetState("Không thể tải danh sách người thuê. Vui lòng thử lại.", true);
			}
			finally
			{
				_isLoading = false;
				btnRefresh.IsEnabled = true;
			}
		}

		// Nạp combo lọc tòa / trạng thái / phòng
		private void PopulateFilters()
		{
			cbBuilding.ItemsSource = new[] { All }
				.Concat(_tenants.Select(t => t.BuildingName).Distinct().OrderBy(name => name));
			cbContractStatus.ItemsSource = new[] { All }
				.Concat(_tenants.Select(t => t.ContractStatusDisplay).Distinct().OrderBy(status => status));
			cbBuilding.SelectedIndex = 0;
			cbContractStatus.SelectedIndex = 0;
			PopulateRoomFilter();
		}

		// Cập nhật KPI tổng / chính / phụ / sắp hết hạn
		private void UpdateSummary()
		{
			lbTotalTenants.Text = _tenants.Select(tenant => tenant.TenantId).Distinct().Count().ToString();
			lbMainTenants.Text = _tenants.Count(tenant => tenant.IsMainTenant).ToString();
			lbRoommates.Text = _tenants.Count(tenant => !tenant.IsMainTenant).ToString();
			lbExpiringTenants.Text = _tenants.Count(tenant =>
				tenant.ContractStatusDisplay == "Sắp hết hạn").ToString();
		}

		// Lọc phòng theo tòa nhà đang chọn
		private void PopulateRoomFilter()
		{
			string? building = cbBuilding.SelectedItem as string;
			var rooms = _tenants
				.Where(t => building == null || building == All || t.BuildingName == building)
				.Select(t => t.RoomNumber)
				.Distinct()
				.OrderBy(room => room);
			cbRoom.ItemsSource = new[] { All }.Concat(rooms);
			cbRoom.SelectedIndex = 0;
		}

		// Điều kiện lọc theo từ khóa và combo
		private bool FilterTenant(object item)
		{
			if (item is not TenantDto tenant)
			{
				return false;
			}

			string keyword = txtSearch.Text.Trim();
			bool matchesKeyword = string.IsNullOrEmpty(keyword) ||
				tenant.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				tenant.IdCardNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				tenant.PhoneNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				(tenant.Email?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
				tenant.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				tenant.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			bool matchesBuilding = cbBuilding.SelectedItem is not string building ||
				building == All || tenant.BuildingName == building;
			bool matchesRoom = cbRoom.SelectedItem is not string room ||
				room == All || tenant.RoomNumber == room;
			bool matchesStatus = cbContractStatus.SelectedItem is not string status ||
				status == All || tenant.ContractStatusDisplay == status;

			return matchesKeyword && matchesBuilding && matchesRoom && matchesStatus;
		}

		// Đổi tòa nhà → làm mới danh sách phòng rồi lọc lại
		private void BuildingFilterChanged(object sender, RoutedEventArgs e)
		{
			if (!_isLoading)
			{
				PopulateRoomFilter();
				RefreshView();
			}
		}

		// Áp lại bộ lọc khi tìm kiếm / combo đổi
		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		// Làm mới view và đếm số dòng hiển thị
		private void RefreshView()
		{
			_tenantView?.Refresh();
			int count = _tenantView?.Cast<object>().Count() ?? 0;
			lbResultCount.Text = $"{count} người thuê";
			SetState(count == 0 ? "Không có người thuê phù hợp." : string.Empty, count == 0);
		}

		// Hiện / ẩn thông báo trạng thái trống
		private void SetState(string message, bool isVisible)
		{
			lbState.Text = message;
			brdState.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}

		// Nhắc chọn dòng trước khi thao tác
		private static void ShowSelectMessage(string message) =>
			MessageBox.Show(message, "Chưa chọn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}
