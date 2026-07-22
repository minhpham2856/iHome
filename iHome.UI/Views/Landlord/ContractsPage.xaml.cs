using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Services;
using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// quản lý hợp đồng — master grid contract + detail khách trên HĐ; filter property/building/search
	public partial class ContractsPage : Page
	{
		private readonly User _currentUser;
		private readonly IHomeDbContext _db;
		private readonly LandlordContractService _service;
		private List<ContractDto> _contracts = new();
		private bool _suppressFilterEvents;

		public ContractsPage(User user)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			// Create one EF Core context shared by services on this page
			_db = new IHomeDbContext();
			// Assign local/page state inside ContractsPage without altering business rules
			_service = new LandlordContractService(_db);
			// Subscribe Unloaded to dispose shared DbContext when page leaves tree
			Unloaded += ContractsPage_Unloaded;
			// Execute UI step inside ContractsPage
			LoadPropertyFilter();
		}

		private void ContractsPage_Unloaded(object sender, RoutedEventArgs e)
		{
			// Unsubscribe Unloaded handler after single cleanup
			Unloaded -= ContractsPage_Unloaded;
			// Dispose shared IHomeDbContext to release SQL Server connection
			_db.Dispose();
		}

		private PropertyFilterOptionDto? SelectedProperty => cbProperty.SelectedItem as PropertyFilterOptionDto;
		private BuildingFilterOptionDto? SelectedBuilding => cbBuilding.SelectedItem as BuildingFilterOptionDto;
		private ContractDto? SelectedContract => dgContracts.SelectedItem as ContractDto;

		private void LoadPropertyFilter(int? keepPropertyId = null, int? keepBuildingId = null)
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = true;
				// Call page BLL service GetPropertyOptions to load or mutate scoped data
				var properties = _service.GetPropertyOptions(_currentUser.Id);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				cbProperty.ItemsSource = properties;
				// Guard clause: only continue when UI selection, role, or input is valid
				if (keepPropertyId.HasValue)
				{
					// Restore or set combo selection to match entity id or filter
					cbProperty.SelectedItem = properties.FirstOrDefault(p => p.Id == keepPropertyId.Value);
				}
				// Alternate branch when previous condition was not satisfied
				else if (properties.Count > 0)
				{
					// Pick default combo index (usually first/all option) after reload
					cbProperty.SelectedIndex = 0;
				}
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = false;
				// Execute UI step inside LoadPropertyFilter
				LoadBuildingFilter(keepBuildingId);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception)
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = false;
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Không thể tải danh sách nhà trọ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void LoadBuildingFilter(int? keepBuildingId = null)
		{
			// Assign local/page state inside LoadBuildingFilter without altering business rules
			int propertyId = SelectedProperty?.Id ?? 0;
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = true;
				// Call page BLL service GetBuildingOptions to load or mutate scoped data
				var buildings = _service.GetBuildingOptions(_currentUser.Id, propertyId);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				cbBuilding.ItemsSource = buildings;
				// Guard clause: only continue when UI selection, role, or input is valid
				if (keepBuildingId.HasValue)
				{
					// Restore or set combo selection to match entity id or filter
					cbBuilding.SelectedItem = buildings.FirstOrDefault(b => b.Id == keepBuildingId.Value);
				}
				// Alternate branch when previous condition was not satisfied
				else if (buildings.Count > 0)
				{
					// Pick default combo index (usually first/all option) after reload
					cbBuilding.SelectedIndex = 0;
				}
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = false;
				// Execute UI step inside LoadBuildingFilter
				LoadContracts();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception)
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = false;
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Không thể tải danh sách tòa nhà.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// BLL filter theo property/building; search keyword client-side trong ApplyFilter
		private void LoadContracts(int? keepSelectedId = null)
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Assign local/page state inside LoadContracts without altering business rules
				int? propertyId = SelectedProperty?.Id is > 0 and int pid ? pid : null;
				// Assign local/page state inside LoadContracts without altering business rules
				int? buildingId = SelectedBuilding?.Id is > 0 and int bid ? bid : null;
				// Call page BLL service GetByLandlord to load or mutate scoped data
				_contracts = _service.GetByLandlord(_currentUser.Id, propertyId, buildingId);
				// Call helper ApplyFilter to refresh UI state from BLL data
				ApplyFilter(keepSelectedId);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Không thể tải danh sách hợp đồng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void ApplyFilter(int? keepSelectedId = null)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			string keyword = (txtSearch?.Text ?? string.Empty).Trim();
			// Assign local/page state inside ApplyFilter without altering business rules
			var filtered = string.IsNullOrEmpty(keyword)
				// Execute UI step inside ApplyFilter
				? _contracts
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				: _contracts.Where(c =>
					// Execute UI step inside ApplyFilter
					c.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					// Execute UI step inside ApplyFilter
					|| c.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					// Execute UI step inside ApplyFilter
					|| c.PropertyName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					// Execute UI step inside ApplyFilter
					|| c.MainTenantName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					// Materialize query to List for repeated binding and filtering
					|| c.StatusDisplay.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgContracts.ItemsSource = filtered;
			// Show or hide panel/border for empty state or role-specific UI
			brdContractState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbContractState.Text = string.IsNullOrEmpty(keyword)
				// Execute UI step inside ApplyFilter
				? "Chưa có hợp đồng trong phạm vi đã chọn."
				// Execute UI step inside ApplyFilter
				: "Không tìm thấy hợp đồng phù hợp.";

			// Guard clause: only continue when UI selection, role, or input is valid
			if (keepSelectedId.HasValue)
			{
				// Restore or set combo selection to match entity id or filter
				dgContracts.SelectedItem = filtered.FirstOrDefault(c => c.Id == keepSelectedId.Value);
			}

			// Restore or set combo selection to match entity id or filter
			if (dgContracts.SelectedItem == null)
			{
				// Execute UI step inside ApplyFilter
				ClearTenants();
			}

			// Execute UI step inside ApplyFilter
			UpdateButtons();
		}

		// panel phải — danh sách tenant trên hợp đồng đang chọn
		private void LoadTenants()
		{
			// Assign local/page state inside LoadTenants without altering business rules
			var contract = SelectedContract;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (contract == null)
			{
				// Execute UI step inside LoadTenants
				ClearTenants();
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetTenants to load or mutate scoped data
				var tenants = _service.GetTenants(_currentUser.Id, contract.Id);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgTenants.ItemsSource = tenants;
				// Show or hide panel/border for empty state or role-specific UI
				brdTenantState.Visibility = tenants.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				// Guard clause: only continue when UI selection, role, or input is valid
				if (tenants.Count == 0)
				{
					// Update TextBlock/TextBox caption or read user-entered text from control
					((TextBlock)brdTenantState.Child).Text = "Hợp đồng chưa có khách thuê.";
				}
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbTenantSection.Text = $"Khách trên hợp đồng - Phòng {contract.RoomNumber}";
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Execute UI step inside LoadTenants
				ClearTenants();
			}
		}

		private void ClearTenants()
		{
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgTenants.ItemsSource = null;
			// Show or hide panel/border for empty state or role-specific UI
			brdTenantState.Visibility = Visibility.Visible;
			// Update TextBlock/TextBox caption or read user-entered text from control
			((TextBlock)brdTenantState.Child).Text = "Chọn một hợp đồng để xem danh sách khách.";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTenantSection.Text = "Khách trên hợp đồng";
		}

		private void UpdateButtons() =>
			btnEdit.IsEnabled = SelectedContract != null;

		private void cbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_suppressFilterEvents) return;
			// Execute UI step inside cbProperty_SelectionChanged
			LoadBuildingFilter();
		}

		private void cbBuilding_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_suppressFilterEvents) return;
			// Execute UI step inside cbBuilding_SelectionChanged
			LoadContracts(SelectedContract?.Id);
		}

		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) =>
			ApplyFilter(SelectedContract?.Id);

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadPropertyFilter(SelectedProperty?.Id, SelectedBuilding?.Id);

		private void dgContracts_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Execute UI step inside dgContracts_SelectionChanged
			UpdateButtons();
			// Call helper LoadTenants to refresh UI state from BLL data
			LoadTenants();
		}

		private void dgContracts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedContract != null) OpenEdit();
		}

		// Assign local/page state inside btnEdit_Click without altering business rules
		private void btnEdit_Click(object sender, RoutedEventArgs e) => OpenEdit();

		private void OpenEdit()
		{
			// Assign local/page state inside OpenEdit without altering business rules
			var selected = SelectedContract;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (selected == null) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetForm to load or mutate scoped data
				var form = _service.GetForm(_currentUser.Id, selected.Id);
				// Guard clause: only continue when UI selection, role, or input is valid
				if (form == null)
				{
					// Show Vietnamese MessageBox to confirm, warn, or report success/failure
					MessageBox.Show("Không tìm thấy hợp đồng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
					// Exit method early or return value/tuple to caller
					return;
				}

				// Call page BLL service GetStatusOptions to load or mutate scoped data
				var dialog = new ContractDialog(form, _service.GetStatusOptions())
				{
					// Resolve parent Window so modal dialogs center on the app shell
					Owner = Window.GetWindow(this)
				};
				// Show modal dialog and block until user confirms or cancels
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				// Call page BLL service Update to load or mutate scoped data
				_service.Update(_currentUser.Id, dialog.Result);
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã cập nhật hợp đồng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
				// Execute UI step inside OpenEdit
				LoadContracts(selected.Id);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}
