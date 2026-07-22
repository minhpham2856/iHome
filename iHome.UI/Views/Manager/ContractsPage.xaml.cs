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

		public ContractsPage(User currentUser, int? buildingId)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside ContractsPage without altering business rules
			_currentUser = currentUser;
			// Assign local/page state inside ContractsPage without altering business rules
			_buildingId = buildingId;
			// Subscribe page Loaded event to defer BLL calls until controls exist
			Loaded += (_, _) => Load();
		}

		// Assign local/page state inside btnRefresh_Click without altering business rules
		private void btnRefresh_Click(object sender, RoutedEventArgs e) => Load();

		private void Load()
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_isLoading) return;
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isLoading = true;
				// Enable/disable control during loading or when prerequisites missing
				btnRefresh.IsEnabled = false;
				// Call helper SetState to refresh UI state from BLL data
				SetState("Đang tải danh sách hợp đồng...", true);
				// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				// Call page BLL service GetContracts to load or mutate scoped data
				var (ok, contracts, error) = ManagerUi.TryGet(() => _service.GetContracts(managerId, _buildingId));
				// Guard clause: only continue when UI selection, role, or input is valid
				if (!ok || contracts == null)
				{
					// Call helper SetState to refresh UI state from BLL data
					SetState(error ?? "Không thể tải danh sách hợp đồng.", true);
					// Exit method early or return value/tuple to caller
					return;
				}
				// Assign local/page state inside Load without altering business rules
				_contracts = contracts;
				// Wrap list in ICollectionView to enable client-side Filter
				_view = CollectionViewSource.GetDefaultView(_contracts);
				// Attach filter predicate so grid hides rows not matching search/combos
				_view.Filter = FilterContract;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgContracts.ItemsSource = _view;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				cbStatus.ItemsSource = new[] { All }.Concat(_contracts.Select(item => item.StatusDisplay).Distinct());
				// Pick default combo index (usually first/all option) after reload
				cbStatus.SelectedIndex = 0;
				// Call helper UpdateSummary to refresh UI state from BLL data
				UpdateSummary();
				// Call helper RefreshView to refresh UI state from BLL data
				RefreshView();
			}
			// Enable/disable control during loading or when prerequisites missing
			finally { _isLoading = false; btnRefresh.IsEnabled = true; }
		}

		private void UpdateSummary()
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTotal.Text = _contracts.Count.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbActive.Text = _contracts.Count(item => item.Status == "Active").ToString();
			// Đếm theo StatusDisplay đã tính thời gian thực (còn < 1 tháng → Sắp hết hạn)
			lbExpiring.Text = _contracts.Count(item => item.StatusDisplay == "Sắp hết hạn").ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTerminated.Text = _contracts.Count(item => item.Status == "Terminated").ToString();
		}

		private bool FilterContract(object item)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (item is not ContractDto contract) return false;
			// Update TextBlock/TextBox caption or read user-entered text from control
			string keyword = txtSearch.Text.Trim();
			// Assign local/page state inside FilterContract without altering business rules
			bool search = string.IsNullOrEmpty(keyword)
				// Execute UI step inside FilterContract
				|| contract.Id.ToString().Contains(keyword)
				// Execute UI step inside FilterContract
				|| contract.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
				// Execute UI step inside FilterContract
				|| contract.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
				// Execute UI step inside FilterContract
				|| contract.MainTenantName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
				// Execute UI step inside FilterContract
				|| contract.TenantNames.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			// Change combo selection to drive filter cascade or dialog default
			bool status = cbStatus.SelectedItem is not string selected || selected == All || selected == contract.StatusDisplay;
			// Exit method early or return value/tuple to caller
			return search && status;
		}

		// Re-run ICollectionView filter and update visible row count label
		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();
		private void RefreshView() { _view?.Refresh(); int count = _view?.Cast<object>().Count() ?? 0; lbResultCount.Text = $"{count} hợp đồng"; SetState(count == 0 ? "Không có hợp đồng phù hợp." : string.Empty, count == 0); }

		private void AddContract_Click(object sender, RoutedEventArgs e)
		{
			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// Call page BLL service GetRoomOptions to load or mutate scoped data
			var (roomsOk, rooms, roomsError) = ManagerUi.TryGet(() => _service.GetRoomOptions(managerId, _buildingId));
			// TenantService.GetTenantOptions manages tenant records and lookup options
			var (tenantsOk, tenants, tenantsError) = ManagerUi.TryGet(() => _tenantService.GetTenantOptions(managerId));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!roomsOk || !tenantsOk || rooms == null || tenants == null)
			{
				// ManagerUi.ShowError shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowError(roomsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Resolve parent Window so modal dialogs center on the app shell
			var dialog = new ContractDialog(rooms, tenants) { Owner = Window.GetWindow(this) };
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				// Call page BLL service CreateContract to load or mutate scoped data
				if (ManagerUi.TryRun(() => { _service.CreateContract(managerId, dialog.Result); }))
				{
					// Execute UI step inside AddContract_Click
					Load();
				}
			}
		}

		private void EditContract_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (dgContracts.SelectedItem is not ContractDto selected) { ShowSelect(); return; }
			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// Call page BLL service GetContract to load or mutate scoped data
			var (formOk, form, formError) = ManagerUi.TryGet(() => _service.GetContract(managerId, selected.Id));
			// Call page BLL service GetRoomOptions to load or mutate scoped data
			var (roomsOk, rooms, roomsError) = ManagerUi.TryGet(() => _service.GetRoomOptions(managerId, _buildingId));
			// TenantService.GetTenantOptions manages tenant records and lookup options
			var (tenantsOk, tenants, tenantsError) = ManagerUi.TryGet(() => _tenantService.GetTenantOptions(managerId));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!formOk || !roomsOk || !tenantsOk || form == null || rooms == null || tenants == null)
			{
				// ManagerUi.ShowError shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowError(formError ?? roomsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Resolve parent Window so modal dialogs center on the app shell
			var dialog = new ContractDialog(rooms, tenants, form) { Owner = Window.GetWindow(this) };
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				// Call page BLL service UpdateContract to load or mutate scoped data
				if (ManagerUi.TryRun(() => _service.UpdateContract(managerId, dialog.Result)))
				{
					// Execute UI step inside EditContract_Click
					Load();
				}
			}
		}

		private void AssignTenant_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			int? selectedContractId = (dgContracts.SelectedItem as ContractDto)?.Id;
			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// Call page BLL service GetContractOptions to load or mutate scoped data
			var (contractsOk, contracts, contractsError) = ManagerUi.TryGet(() => _service.GetContractOptions(managerId, _buildingId));
			// TenantService.GetTenantOptions manages tenant records and lookup options
			var (tenantsOk, tenants, tenantsError) = ManagerUi.TryGet(() => _tenantService.GetTenantOptions(managerId));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!contractsOk || !tenantsOk || contracts == null || tenants == null)
			{
				// ManagerUi.ShowError shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowError(contractsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Resolve parent Window so modal dialogs center on the app shell
			var dialog = new AssignTenantDialog(contracts, tenants, null, selectedContractId) { Owner = Window.GetWindow(this) };
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true)
			{
				// Call page BLL service AssignTenant to load or mutate scoped data
				if (ManagerUi.TryRun(() => _service.AssignTenant(managerId, dialog.ContractId, dialog.TenantId, dialog.IsMainTenant)))
				{
					// Execute UI step inside AssignTenant_Click
					Load();
				}
			}
		}

		private void DeleteContract_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (dgContracts.SelectedItem is not ContractDto selected) { ShowSelect(); return; }
			// Show Vietnamese MessageBox to confirm, warn, or report success/failure
			if (MessageBox.Show($"Xóa hợp đồng #{selected.Id}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// Call page BLL service DeleteContract to load or mutate scoped data
			if (ManagerUi.TryRun(() => _service.DeleteContract(managerId, selected.Id)))
			{
				// Execute UI step inside DeleteContract_Click
				Load();
			}
		}

		private void SetState(string message, bool visible) { lbState.Text = message; brdState.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; }
		private static void ShowSelect() => MessageBox.Show("Vui lòng chọn hợp đồng.", "Chưa chọn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}
