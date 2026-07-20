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
	public partial class ContractsPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _propertyId;
		private readonly ManagerContractService _service = new();
		private readonly ManagerTenantService _tenantService = new();
		private List<ManagerContractDto> _contracts = new();
		private ICollectionView? _view;
		private bool _isLoading;

		public ContractsPage(User currentUser, int? propertyId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_propertyId = propertyId;
			Loaded += async (_, _) => await LoadAsync();
		}

		private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

		private async Task LoadAsync()
		{
			if (_isLoading) return;
			try
			{
				_isLoading = true;
				BtnRefresh.IsEnabled = false;
				SetState("Đang tải danh sách hợp đồng...", true);
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				var (ok, contracts, error) = await ManagerUi.TryGetAsync(() => _service.GetContracts(managerId, _propertyId));
				if (!ok || contracts == null)
				{
					SetState(error ?? "Không thể tải danh sách hợp đồng.", true);
					return;
				}
				_contracts = contracts;
				_view = CollectionViewSource.GetDefaultView(_contracts);
				_view.Filter = FilterContract;
				ContractsGrid.ItemsSource = _view;
				CboStatus.ItemsSource = new[] { All }.Concat(_contracts.Select(item => item.StatusDisplay).Distinct());
				CboStatus.SelectedIndex = 0;
				UpdateSummary();
				RefreshView();
			}
			finally { _isLoading = false; BtnRefresh.IsEnabled = true; }
		}

		private void UpdateSummary()
		{
			TxtTotal.Text = _contracts.Count.ToString();
			TxtActive.Text = _contracts.Count(item => item.Status == "Active").ToString();
			TxtExpiring.Text = _contracts.Count(item => item.StatusDisplay == "Sắp hết hạn").ToString();
			TxtTerminated.Text = _contracts.Count(item => item.Status == "Terminated").ToString();
		}

		private bool FilterContract(object item)
		{
			if (item is not ManagerContractDto contract) return false;
			string keyword = TxtSearch.Text.Trim();
			bool search = string.IsNullOrEmpty(keyword) || contract.Id.ToString().Contains(keyword) || contract.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase) || contract.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) || contract.MainTenantName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			bool status = CboStatus.SelectedItem is not string selected || selected == All || selected == contract.StatusDisplay;
			return search && status;
		}

		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();
		private void RefreshView() { _view?.Refresh(); int count = _view?.Cast<object>().Count() ?? 0; TxtResultCount.Text = $"{count} hợp đồng"; SetState(count == 0 ? "Không có hợp đồng phù hợp." : string.Empty, count == 0); }

		private async void AddContract_Click(object sender, RoutedEventArgs e)
		{
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (roomsOk, rooms, roomsError) = await ManagerUi.TryGetAsync(() => _service.GetRoomOptions(managerId, _propertyId));
			var (tenantsOk, tenants, tenantsError) = await ManagerUi.TryGetAsync(() => _tenantService.GetTenantOptions(managerId));
			if (!roomsOk || !tenantsOk || rooms == null || tenants == null)
			{
				ManagerUi.ShowError(roomsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				return;
			}
			var dialog = new ContractDialog(rooms, tenants) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				if (await ManagerUi.TryRunAsync(() => { _service.CreateContract(managerId, dialog.Result); }))
				{
					await LoadAsync();
				}
			}
		}

		private async void EditContract_Click(object sender, RoutedEventArgs e)
		{
			if (ContractsGrid.SelectedItem is not ManagerContractDto selected) { ShowSelect(); return; }
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (formOk, form, formError) = await ManagerUi.TryGetAsync(() => _service.GetContract(managerId, selected.Id));
			var (roomsOk, rooms, roomsError) = await ManagerUi.TryGetAsync(() => _service.GetRoomOptions(managerId, _propertyId));
			var (tenantsOk, tenants, tenantsError) = await ManagerUi.TryGetAsync(() => _tenantService.GetTenantOptions(managerId));
			if (!formOk || !roomsOk || !tenantsOk || form == null || rooms == null || tenants == null)
			{
				ManagerUi.ShowError(formError ?? roomsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				return;
			}
			var dialog = new ContractDialog(rooms, tenants, form) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				if (await ManagerUi.TryRunAsync(() => _service.UpdateContract(managerId, dialog.Result)))
				{
					await LoadAsync();
				}
			}
		}

		private async void AssignTenant_Click(object sender, RoutedEventArgs e)
		{
			int? selectedContractId = (ContractsGrid.SelectedItem as ManagerContractDto)?.Id;
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (contractsOk, contracts, contractsError) = await ManagerUi.TryGetAsync(() => _service.GetContractOptions(managerId, _propertyId));
			var (tenantsOk, tenants, tenantsError) = await ManagerUi.TryGetAsync(() => _tenantService.GetTenantOptions(managerId));
			if (!contractsOk || !tenantsOk || contracts == null || tenants == null)
			{
				ManagerUi.ShowError(contractsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				return;
			}
			var dialog = new AssignTenantDialog(contracts, tenants, null, selectedContractId) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true)
			{
				if (await ManagerUi.TryRunAsync(() => _service.AssignTenant(managerId, dialog.ContractId, dialog.TenantId, dialog.IsMainTenant)))
				{
					await LoadAsync();
				}
			}
		}

		private async void DeleteContract_Click(object sender, RoutedEventArgs e)
		{
			if (ContractsGrid.SelectedItem is not ManagerContractDto selected) { ShowSelect(); return; }
			if (MessageBox.Show($"Xóa hợp đồng #{selected.Id}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			if (await ManagerUi.TryRunAsync(() => _service.DeleteContract(managerId, selected.Id)))
			{
				await LoadAsync();
			}
		}

		private void SetState(string message, bool visible) { StateText.Text = message; StatePanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; }
		private static void ShowSelect() => MessageBox.Show("Vui lòng chọn hợp đồng.", "Chưa chọn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}
