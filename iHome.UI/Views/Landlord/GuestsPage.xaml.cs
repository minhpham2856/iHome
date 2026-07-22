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
	// quản lý khách thuê — master grid tenant + detail hợp đồng; filter property/building/search client-side
	public partial class GuestsPage : Page
	{
		private readonly User _currentUser;
		private readonly IHomeDbContext _db;
		private readonly LandlordTenantService _service;
		private List<TenantDto> _tenants = new();
		private bool _suppressFilterEvents;

		public GuestsPage(User user)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			// Create one EF Core context shared by services on this page
			_db = new IHomeDbContext();
			// Assign local/page state inside GuestsPage without altering business rules
			_service = new LandlordTenantService(_db);
			// Subscribe Unloaded to dispose shared DbContext when page leaves tree
			Unloaded += GuestsPage_Unloaded;
			// Execute UI step inside GuestsPage
			LoadPropertyFilter();
		}

		private void GuestsPage_Unloaded(object sender, RoutedEventArgs e)
		{
			// Unsubscribe Unloaded handler after single cleanup
			Unloaded -= GuestsPage_Unloaded;
			// Dispose shared IHomeDbContext to release SQL Server connection
			_db.Dispose();
		}

		private PropertyFilterOptionDto? SelectedProperty => cbProperty.SelectedItem as PropertyFilterOptionDto;
		private BuildingFilterOptionDto? SelectedBuilding => cbBuilding.SelectedItem as BuildingFilterOptionDto;
		private TenantDto? SelectedTenant => dgTenants.SelectedItem as TenantDto;

		// cascade: property combo → building combo → LoadTenants
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
				// Call helper LoadTenants to refresh UI state from BLL data
				LoadTenants();
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

		// load full list từ BLL rồi ApplyFilter in-memory
		private void LoadTenants(int? keepSelectedId = null)
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetByLandlord to load or mutate scoped data
				_tenants = _service.GetByLandlord(_currentUser.Id);
				// Call helper ApplyFilter to refresh UI state from BLL data
				ApplyFilter(keepSelectedId);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Không thể tải danh sách khách thuê.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// lọc theo property/building/keyword; cập nhật empty state và selection
		private void ApplyFilter(int? keepSelectedId = null)
		{
			// Assign local/page state inside ApplyFilter without altering business rules
			int propertyId = SelectedProperty?.Id ?? 0;
			// Assign local/page state inside ApplyFilter without altering business rules
			int buildingId = SelectedBuilding?.Id ?? 0;
			// Update TextBlock/TextBox caption or read user-entered text from control
			string keyword = (txtSearch?.Text ?? string.Empty).Trim();

			// Work with BLL DTO/form object returned from service or built from controls
			IEnumerable<TenantDto> query = _tenants;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (propertyId > 0)
			{
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				query = query.Where(t => t.CurrentPropertyId == propertyId);
			}
			// Guard clause: only continue when UI selection, role, or input is valid
			if (buildingId > 0)
			{
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				query = query.Where(t => t.CurrentBuildingId == buildingId);
			}
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!string.IsNullOrEmpty(keyword))
			{
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				query = query.Where(t =>
					// Execute UI step inside ApplyFilter
					t.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					// Execute UI step inside ApplyFilter
					|| t.IdCardNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					// Execute UI step inside ApplyFilter
					|| t.PhoneNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					// Execute UI step inside ApplyFilter
					|| (t.Email?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
					// Execute UI step inside ApplyFilter
					|| t.CurrentBuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					// Execute UI step inside ApplyFilter
					|| t.CurrentRoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase));
			}

			// Materialize query to List for repeated binding and filtering
			var filtered = query.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgTenants.ItemsSource = filtered;
			// Show or hide panel/border for empty state or role-specific UI
			brdTenantState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTenantState.Text = !string.IsNullOrEmpty(keyword) || propertyId > 0 || buildingId > 0
				// Execute UI step inside ApplyFilter
				? "Không tìm thấy khách phù hợp."
				// Execute UI step inside ApplyFilter
				: "Chưa có khách thuê trong hệ thống nhà trọ của bạn.";

			// Guard clause: only continue when UI selection, role, or input is valid
			if (keepSelectedId.HasValue)
			{
				// Restore or set combo selection to match entity id or filter
				dgTenants.SelectedItem = filtered.FirstOrDefault(t => t.Id == keepSelectedId.Value);
			}

			// Restore or set combo selection to match entity id or filter
			if (dgTenants.SelectedItem == null)
			{
				// Execute UI step inside ApplyFilter
				ClearContracts();
			}

			// Execute UI step inside ApplyFilter
			UpdateButtons();
		}

		// panel phải — hợp đồng của tenant đang chọn
		private void LoadContracts()
		{
			// Assign local/page state inside LoadContracts without altering business rules
			var tenant = SelectedTenant;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (tenant == null)
			{
				// Execute UI step inside LoadContracts
				ClearContracts();
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetContracts to load or mutate scoped data
				var contracts = _service.GetContracts(_currentUser.Id, tenant.Id);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgContracts.ItemsSource = contracts;
				// Show or hide panel/border for empty state or role-specific UI
				brdContractState.Visibility = contracts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				// Guard clause: only continue when UI selection, role, or input is valid
				if (contracts.Count == 0)
				{
					// Update TextBlock/TextBox caption or read user-entered text from control
					((TextBlock)brdContractState.Child).Text = "Khách này chưa có hợp đồng trong hệ thống nhà trọ của bạn.";
				}
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbContractSection.Text = $"Hợp đồng - {tenant.FullName}";
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Execute UI step inside LoadContracts
				ClearContracts();
			}
		}

		private void ClearContracts()
		{
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgContracts.ItemsSource = null;
			// Show or hide panel/border for empty state or role-specific UI
			brdContractState.Visibility = Visibility.Visible;
			// Update TextBlock/TextBox caption or read user-entered text from control
			((TextBlock)brdContractState.Child).Text = "Chọn một khách để xem hợp đồng và phòng đang thuê.";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbContractSection.Text = "Hợp đồng";
		}

		private void UpdateButtons() =>
			btnEdit.IsEnabled = SelectedTenant != null;

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
			// Call helper ApplyFilter to refresh UI state from BLL data
			ApplyFilter(SelectedTenant?.Id);
		}

		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) =>
			ApplyFilter(SelectedTenant?.Id);

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadPropertyFilter(SelectedProperty?.Id, SelectedBuilding?.Id);

		private void dgTenants_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Execute UI step inside dgTenants_SelectionChanged
			UpdateButtons();
			// Execute UI step inside dgTenants_SelectionChanged
			LoadContracts();
		}

		private void dgTenants_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedTenant != null) OpenEdit();
		}

		// Assign local/page state inside btnEdit_Click without altering business rules
		private void btnEdit_Click(object sender, RoutedEventArgs e) => OpenEdit();

		// mở TenantDialog — chỉ Update, không Create từ trang này
		private void OpenEdit()
		{
			// Assign local/page state inside OpenEdit without altering business rules
			var selected = SelectedTenant;
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
					MessageBox.Show("Không tìm thấy khách thuê.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
					// Exit method early or return value/tuple to caller
					return;
				}

				// Resolve parent Window so modal dialogs center on the app shell
				var dialog = new TenantDialog(form) { Owner = Window.GetWindow(this) };
				// Show modal dialog and block until user confirms or cancels
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				// Call page BLL service Update to load or mutate scoped data
				_service.Update(_currentUser.Id, dialog.Result);
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã cập nhật khách thuê.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
				// Call helper LoadTenants to refresh UI state from BLL data
				LoadTenants(selected.Id);
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
