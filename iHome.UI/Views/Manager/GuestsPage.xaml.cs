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
	public partial class GuestsPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _buildingId;
		private readonly ManagerTenantService _service = new();
		private readonly ManagerContractService _contractService = new();
		private List<ManagerTenantDto> _tenants = new();
		private ICollectionView? _tenantView;
		private bool _isLoading;

		public GuestsPage(User currentUser, int? buildingId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_buildingId = buildingId;
			Loaded += GuestsPage_Loaded;
		}

		private async void GuestsPage_Loaded(object sender, RoutedEventArgs e) =>
			await LoadTenantsAsync();

		private async void BtnRefresh_Click(object sender, RoutedEventArgs e) =>
			await LoadTenantsAsync();

		private async void AddTenant_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new TenantDialog { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() != true || dialog.Result == null)
			{
				return;
			}

			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (ok, _) = await ManagerUi.TryRunAsync(() => _service.CreateTenant(managerId, dialog.Result));
			if (!ok)
			{
				return;
			}
			await LoadTenantsAsync();
			// Không gán phòng ngay: khách chỉ vào phòng khi tạo hợp đồng (đủ người đứng tên)
			MessageBox.Show(
				"Đã thêm khách. Hãy tạo hợp đồng để gắn khách vào phòng.",
				"Thêm khách thành công",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private async void EditTenant_Click(object sender, RoutedEventArgs e)
		{
			if (GuestsGrid.SelectedItem is not ManagerTenantDto selected)
			{
				ShowSelectMessage("Vui lòng chọn khách cần sửa.");
				return;
			}

			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (formOk, form, formError) = await ManagerUi.TryGetAsync(() => _service.GetTenant(managerId, selected.TenantId));
			if (!formOk || form == null)
			{
				ManagerUi.ShowError(formError ?? "Không thể tải thông tin khách.");
				return;
			}
			var dialog = new TenantDialog(form) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				if (await ManagerUi.TryRunAsync(() => _service.UpdateTenant(managerId, dialog.Result)))
				{
					await LoadTenantsAsync();
				}
			}
		}

		private async void DeleteTenant_Click(object sender, RoutedEventArgs e)
		{
			if (GuestsGrid.SelectedItem is not ManagerTenantDto selected)
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
			if (await ManagerUi.TryRunAsync(() => _service.DeleteTenant(managerId, selected.TenantId)))
			{
				await LoadTenantsAsync();
			}
		}

		private async void AssignTenant_Click(object sender, RoutedEventArgs e)
		{
			int? tenantId = (GuestsGrid.SelectedItem as ManagerTenantDto)?.TenantId;
			await ShowAssignDialogAsync(tenantId);
		}

		private async Task ShowAssignDialogAsync(int? selectedTenantId)
		{
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (contractsOk, contracts, contractsError) = await ManagerUi.TryGetAsync(() => _contractService.GetContractOptions(managerId, _buildingId));
			var (tenantsOk, tenants, tenantsError) = await ManagerUi.TryGetAsync(() => _service.GetTenantOptions(managerId));
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
				if (await ManagerUi.TryRunAsync(() => _contractService.AssignTenant(
					managerId,
					dialog.ContractId,
					dialog.TenantId,
					dialog.IsMainTenant)))
				{
					await LoadTenantsAsync();
				}
			}
		}

		private async void RemoveTenant_Click(object sender, RoutedEventArgs e)
		{
			if (GuestsGrid.SelectedItem is not ManagerTenantDto selected || selected.ContractId <= 0)
			{
				ShowSelectMessage("Vui lòng chọn một khách đang thuộc hợp đồng.");
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
			if (await ManagerUi.TryRunAsync(() => _contractService.RemoveTenant(managerId, selected.ContractId, selected.TenantId)))
			{
				await LoadTenantsAsync();
			}
		}

		private async Task LoadTenantsAsync()
		{
			if (_isLoading)
			{
				return;
			}

			try
			{
				_isLoading = true;
				BtnRefresh.IsEnabled = false;
				SetState("Đang tải danh sách người thuê...", true);
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				_tenants = await Task.Run(() => _service.GetTenants(managerId, _buildingId));

				_tenantView = CollectionViewSource.GetDefaultView(_tenants);
				_tenantView.Filter = FilterTenant;
				GuestsGrid.ItemsSource = _tenantView;
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
				BtnRefresh.IsEnabled = true;
			}
		}

		private void PopulateFilters()
		{
			CbBuilding.ItemsSource = new[] { All }
				.Concat(_tenants.Select(t => t.BuildingName).Distinct().OrderBy(name => name));
			CbContractStatus.ItemsSource = new[] { All }
				.Concat(_tenants.Select(t => t.ContractStatusDisplay).Distinct().OrderBy(status => status));
			CbBuilding.SelectedIndex = 0;
			CbContractStatus.SelectedIndex = 0;
			PopulateRoomFilter();
		}

		private void UpdateSummary()
		{
			TxtTotalTenants.Text = _tenants.Select(tenant => tenant.TenantId).Distinct().Count().ToString();
			TxtMainTenants.Text = _tenants.Count(tenant => tenant.IsMainTenant).ToString();
			TxtRoommates.Text = _tenants.Count(tenant => !tenant.IsMainTenant).ToString();
			TxtExpiringTenants.Text = _tenants.Count(tenant =>
				tenant.ContractStatusDisplay == "Sắp hết hạn").ToString();
		}

		private void PopulateRoomFilter()
		{
			string? building = CbBuilding.SelectedItem as string;
			var rooms = _tenants
				.Where(t => building == null || building == All || t.BuildingName == building)
				.Select(t => t.RoomNumber)
				.Distinct()
				.OrderBy(room => room);
			CbRoom.ItemsSource = new[] { All }.Concat(rooms);
			CbRoom.SelectedIndex = 0;
		}

		private bool FilterTenant(object item)
		{
			if (item is not ManagerTenantDto tenant)
			{
				return false;
			}

			string keyword = TxtSearch.Text.Trim();
			bool matchesKeyword = string.IsNullOrEmpty(keyword) ||
				tenant.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				tenant.IdCardNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				tenant.PhoneNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				(tenant.Email?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
				tenant.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				tenant.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			bool matchesBuilding = CbBuilding.SelectedItem is not string building ||
				building == All || tenant.BuildingName == building;
			bool matchesRoom = CbRoom.SelectedItem is not string room ||
				room == All || tenant.RoomNumber == room;
			bool matchesStatus = CbContractStatus.SelectedItem is not string status ||
				status == All || tenant.ContractStatusDisplay == status;

			return matchesKeyword && matchesBuilding && matchesRoom && matchesStatus;
		}

		private void BuildingFilterChanged(object sender, RoutedEventArgs e)
		{
			if (!_isLoading)
			{
				PopulateRoomFilter();
				RefreshView();
			}
		}

		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		private void RefreshView()
		{
			_tenantView?.Refresh();
			int count = _tenantView?.Cast<object>().Count() ?? 0;
			TxtResultCount.Text = $"{count} người thuê";
			SetState(count == 0 ? "Không có người thuê phù hợp." : string.Empty, count == 0);
		}

		private void SetState(string message, bool isVisible)
		{
			StateText.Text = message;
			StatePanel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}

		private static void ShowSelectMessage(string message) =>
			MessageBox.Show(message, "Chưa chọn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}
