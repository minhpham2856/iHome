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

		public GuestsPage(User currentUser, int? buildingId)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside GuestsPage without altering business rules
			_currentUser = currentUser;
			// Assign local/page state inside GuestsPage without altering business rules
			_buildingId = buildingId;
			// Subscribe page Loaded event to defer BLL calls until controls exist
			Loaded += GuestsPage_Loaded;
		}

		private void GuestsPage_Loaded(object sender, RoutedEventArgs e) =>
			LoadTenants();

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadTenants();

		private void AddTenant_Click(object sender, RoutedEventArgs e)
		{
			// Resolve parent Window so modal dialogs center on the app shell
			var dialog = new TenantDialog { Owner = Window.GetWindow(this) };
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() != true || dialog.Result == null)
			{
				// Exit method early or return value/tuple to caller
				return;
			}

			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// Call page BLL service CreateTenant to load or mutate scoped data
			var (ok, _) = ManagerUi.TryRun(() => _service.CreateTenant(managerId, dialog.Result));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!ok)
			{
				// Exit method early or return value/tuple to caller
				return;
			}
			// Call helper LoadTenants to refresh UI state from BLL data
			LoadTenants();
			// Không gán phòng ngay: khách chỉ vào phòng khi tạo hợp đồng (đủ người đứng tên)
			MessageBox.Show(
				// Execute UI step inside AddTenant_Click
				"Đã thêm khách. Hãy tạo hợp đồng để gắn khách vào phòng.",
				// Execute UI step inside AddTenant_Click
				"Thêm khách thành công",
				// Execute UI step inside AddTenant_Click
				MessageBoxButton.OK,
				// Execute UI step inside AddTenant_Click
				MessageBoxImage.Information);
		}

		private void EditTenant_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (dgGuests.SelectedItem is not TenantDto selected)
			{
				// Execute UI step inside EditTenant_Click
				ShowSelectMessage("Vui lòng chọn khách cần sửa.");
				// Exit method early or return value/tuple to caller
				return;
			}

			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// Call page BLL service GetTenant to load or mutate scoped data
			var (formOk, form, formError) = ManagerUi.TryGet(() => _service.GetTenant(managerId, selected.TenantId));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!formOk || form == null)
			{
				// ManagerUi.ShowError shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowError(formError ?? "Không thể tải thông tin khách.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Resolve parent Window so modal dialogs center on the app shell
			var dialog = new TenantDialog(form) { Owner = Window.GetWindow(this) };
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				// Call page BLL service UpdateTenant to load or mutate scoped data
				if (ManagerUi.TryRun(() => _service.UpdateTenant(managerId, dialog.Result)))
				{
					// Call helper LoadTenants to refresh UI state from BLL data
					LoadTenants();
				}
			}
		}

		private void DeleteTenant_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (dgGuests.SelectedItem is not TenantDto selected)
			{
				// Execute UI step inside DeleteTenant_Click
				ShowSelectMessage("Vui lòng chọn khách cần xóa.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Show Vietnamese MessageBox to confirm, warn, or report success/failure
			if (MessageBox.Show(
				// Execute UI step inside DeleteTenant_Click
				$"Xóa khách {selected.FullName}?",
				// Execute UI step inside DeleteTenant_Click
				"Xác nhận xóa",
				// Execute UI step inside DeleteTenant_Click
				MessageBoxButton.YesNo,
				// Assign local/page state inside DeleteTenant_Click without altering business rules
				MessageBoxImage.Warning) != MessageBoxResult.Yes)
			{
				// Exit method early or return value/tuple to caller
				return;
			}

			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// Call page BLL service DeleteTenant to load or mutate scoped data
			if (ManagerUi.TryRun(() => _service.DeleteTenant(managerId, selected.TenantId)))
			{
				// Call helper LoadTenants to refresh UI state from BLL data
				LoadTenants();
			}
		}

		private void AssignTenant_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			int? tenantId = (dgGuests.SelectedItem as TenantDto)?.TenantId;
			// Execute UI step inside AssignTenant_Click
			ShowAssignDialog(tenantId);
		}

		private void ShowAssignDialog(int? selectedTenantId)
		{
			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// ContractService.GetContractOptions loads contracts and tenant assignment links
			var (contractsOk, contracts, contractsError) = ManagerUi.TryGet(() => _contractService.GetContractOptions(managerId, _buildingId));
			// Call page BLL service GetTenantOptions to load or mutate scoped data
			var (tenantsOk, tenants, tenantsError) = ManagerUi.TryGet(() => _service.GetTenantOptions(managerId));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!contractsOk || !tenantsOk || contracts == null || tenants == null)
			{
				// ManagerUi.ShowError shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowError(contractsError ?? tenantsError ?? "Không thể tải dữ liệu.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Construct modal dialog prefilled with lookup lists or edit DTO
			var dialog = new AssignTenantDialog(contracts, tenants, selectedTenantId)
			{
				// Resolve parent Window so modal dialogs center on the app shell
				Owner = Window.GetWindow(this)
			};
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true)
			{
				// ContractService.AssignTenant loads contracts and tenant assignment links
				if (ManagerUi.TryRun(() => _contractService.AssignTenant(
					// Execute UI step inside ShowAssignDialog
					managerId,
					// Execute UI step inside ShowAssignDialog
					dialog.ContractId,
					// Execute UI step inside ShowAssignDialog
					dialog.TenantId,
					// Execute UI step inside ShowAssignDialog
					dialog.IsMainTenant)))
				{
					// Call helper LoadTenants to refresh UI state from BLL data
					LoadTenants();
				}
			}
		}

		private void RemoveTenant_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (dgGuests.SelectedItem is not TenantDto selected || selected.ContractId <= 0)
			{
				// Execute UI step inside RemoveTenant_Click
				ShowSelectMessage("Vui lòng chọn một khách đang thuê thuộc hợp đồng.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Show Vietnamese MessageBox to confirm, warn, or report success/failure
			if (MessageBox.Show(
				// Execute UI step inside RemoveTenant_Click
				$"Gỡ {selected.FullName} khỏi hợp đồng #{selected.ContractId}?",
				// Execute UI step inside RemoveTenant_Click
				"Xác nhận gỡ khách",
				// Execute UI step inside RemoveTenant_Click
				MessageBoxButton.YesNo,
				// Assign local/page state inside RemoveTenant_Click without altering business rules
				MessageBoxImage.Question) != MessageBoxResult.Yes)
			{
				// Exit method early or return value/tuple to caller
				return;
			}

			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// ContractService.RemoveTenant loads contracts and tenant assignment links
			if (ManagerUi.TryRun(() => _contractService.RemoveTenant(managerId, selected.ContractId, selected.TenantId)))
			{
				// Call helper LoadTenants to refresh UI state from BLL data
				LoadTenants();
			}
		}

		private void LoadTenants()
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_isLoading)
			{
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isLoading = true;
				// Enable/disable control during loading or when prerequisites missing
				btnRefresh.IsEnabled = false;
				// Call helper SetState to refresh UI state from BLL data
				SetState("Đang tải danh sách người thuê...", true);
				// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				// Call page BLL service GetTenants to load or mutate scoped data
				_tenants = _service.GetTenants(managerId, _buildingId);

				// Wrap list in ICollectionView to enable client-side Filter
				_tenantView = CollectionViewSource.GetDefaultView(_tenants);
				// Attach filter predicate so grid hides rows not matching search/combos
				_tenantView.Filter = FilterTenant;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgGuests.ItemsSource = _tenantView;
				// Call helper PopulateFilters to refresh UI state from BLL data
				PopulateFilters();
				// Call helper UpdateSummary to refresh UI state from BLL data
				UpdateSummary();
				// Call helper RefreshView to refresh UI state from BLL data
				RefreshView();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception)
			{
				// Call helper SetState to refresh UI state from BLL data
				SetState("Không thể tải danh sách người thuê. Vui lòng thử lại.", true);
			}
			// Always reset loading flags and re-enable refresh regardless of success
			finally
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isLoading = false;
				// Enable/disable control during loading or when prerequisites missing
				btnRefresh.IsEnabled = true;
			}
		}

		private void PopulateFilters()
		{
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbBuilding.ItemsSource = new[] { All }
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Concat(_tenants.Select(t => t.BuildingName).Distinct().OrderBy(name => name));
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbContractStatus.ItemsSource = new[] { All }
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Concat(_tenants.Select(t => t.ContractStatusDisplay).Distinct().OrderBy(status => status));
			// Pick default combo index (usually first/all option) after reload
			cbBuilding.SelectedIndex = 0;
			// Pick default combo index (usually first/all option) after reload
			cbContractStatus.SelectedIndex = 0;
			// Execute UI step inside PopulateFilters
			PopulateRoomFilter();
		}

		private void UpdateSummary()
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTotalTenants.Text = _tenants.Select(tenant => tenant.TenantId).Distinct().Count().ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbMainTenants.Text = _tenants.Count(tenant => tenant.IsMainTenant).ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbRoommates.Text = _tenants.Count(tenant => !tenant.IsMainTenant).ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbExpiringTenants.Text = _tenants.Count(tenant =>
				// Assign local/page state inside UpdateSummary without altering business rules
				tenant.ContractStatusDisplay == "Sắp hết hạn").ToString();
		}

		private void PopulateRoomFilter()
		{
			// Change combo selection to drive filter cascade or dialog default
			string? building = cbBuilding.SelectedItem as string;
			// Assign local/page state inside PopulateRoomFilter without altering business rules
			var rooms = _tenants
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Where(t => building == null || building == All || t.BuildingName == building)
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Select(t => t.RoomNumber)
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Distinct()
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.OrderBy(room => room);
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbRoom.ItemsSource = new[] { All }.Concat(rooms);
			// Pick default combo index (usually first/all option) after reload
			cbRoom.SelectedIndex = 0;
		}

		private bool FilterTenant(object item)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (item is not TenantDto tenant)
			{
				// Exit method early or return value/tuple to caller
				return false;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			string keyword = txtSearch.Text.Trim();
			// Assign local/page state inside FilterTenant without altering business rules
			bool matchesKeyword = string.IsNullOrEmpty(keyword) ||
				// Execute UI step inside FilterTenant
				tenant.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				// Execute UI step inside FilterTenant
				tenant.IdCardNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				// Execute UI step inside FilterTenant
				tenant.PhoneNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				// Execute UI step inside FilterTenant
				(tenant.Email?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
				// Execute UI step inside FilterTenant
				tenant.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				// Execute UI step inside FilterTenant
				tenant.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			// Change combo selection to drive filter cascade or dialog default
			bool matchesBuilding = cbBuilding.SelectedItem is not string building ||
				// Assign local/page state inside FilterTenant without altering business rules
				building == All || tenant.BuildingName == building;
			// Change combo selection to drive filter cascade or dialog default
			bool matchesRoom = cbRoom.SelectedItem is not string room ||
				// Assign local/page state inside FilterTenant without altering business rules
				room == All || tenant.RoomNumber == room;
			// Change combo selection to drive filter cascade or dialog default
			bool matchesStatus = cbContractStatus.SelectedItem is not string status ||
				// Assign local/page state inside FilterTenant without altering business rules
				status == All || tenant.ContractStatusDisplay == status;

			// Exit method early or return value/tuple to caller
			return matchesKeyword && matchesBuilding && matchesRoom && matchesStatus;
		}

		private void BuildingFilterChanged(object sender, RoutedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!_isLoading)
			{
				// Execute UI step inside BuildingFilterChanged
				PopulateRoomFilter();
				// Call helper RefreshView to refresh UI state from BLL data
				RefreshView();
			}
		}

		// Re-run ICollectionView filter and update visible row count label
		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		private void RefreshView()
		{
			// Reapply ICollectionView filter after search or combo change
			_tenantView?.Refresh();
			// Aggregate list into KPI number shown on summary labels
			int count = _tenantView?.Cast<object>().Count() ?? 0;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbResultCount.Text = $"{count} người thuê";
			// Call helper SetState to refresh UI state from BLL data
			SetState(count == 0 ? "Không có người thuê phù hợp." : string.Empty, count == 0);
		}

		private void SetState(string message, bool isVisible)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbState.Text = message;
			// Show or hide panel/border for empty state or role-specific UI
			brdState.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}

		private static void ShowSelectMessage(string message) =>
			MessageBox.Show(message, "Chưa chọn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}
