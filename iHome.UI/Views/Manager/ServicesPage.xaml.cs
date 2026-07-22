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

		public ServicesPage(User currentUser, int? buildingId)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside ServicesPage without altering business rules
			_currentUser = currentUser;
			// Assign local/page state inside ServicesPage without altering business rules
			_buildingId = buildingId;
			// Subscribe page Loaded event to defer BLL calls until controls exist
			Loaded += ServicesPage_Loaded;
		}

		private void ServicesPage_Loaded(object sender, RoutedEventArgs e) =>
			LoadServices();

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadServices();

		private void LoadServices()
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
				SetState("Đang tải danh sách dịch vụ...", true);
				// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				// Call page BLL service GetServices to load or mutate scoped data
				_services = _service.GetServices(managerId, _buildingId);
				// RoomServiceAssignmentService.GetAssignments toggles room-service links
				_assignments = _assignmentService.GetAssignments(managerId, _buildingId);

				// Wrap list in ICollectionView to enable client-side Filter
				_serviceView = CollectionViewSource.GetDefaultView(_services);
				// Attach filter predicate so grid hides rows not matching search/combos
				_serviceView.Filter = FilterService;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgServices.ItemsSource = _serviceView;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgAssignments.ItemsSource = _assignments;
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
				SetState("Không thể tải danh sách dịch vụ. Vui lòng thử lại.", true);
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

		private void AssignService_Click(object sender, RoutedEventArgs e)
		{
			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// RoomServiceAssignmentService.GetRoomOptions toggles room-service links
			var (roomsOk, rooms, roomsError) = ManagerUi.TryGet(() => _assignmentService.GetRoomOptions(managerId, _buildingId));
			// RoomServiceAssignmentService.GetServiceOptions toggles room-service links
			var (servicesOk, services, servicesError) = ManagerUi.TryGet(() => _assignmentService.GetServiceOptions(managerId, _buildingId));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!roomsOk || !servicesOk || rooms == null || services == null)
			{
				// ManagerUi.ShowError shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowError(roomsError ?? servicesError ?? "Không thể tải dữ liệu.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Resolve parent Window so modal dialogs center on the app shell
			var dialog = new RoomServiceDialog(rooms, services) { Owner = Window.GetWindow(this) };
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true)
			{
				// RoomServiceAssignmentService.SetAssignment toggles room-service links
				if (ManagerUi.TryRun(() => _assignmentService.SetAssignment(managerId, dialog.RoomId, dialog.ServiceId, true)))
				{
					// Call helper LoadServices to refresh UI state from BLL data
					LoadServices();
				}
			}
		}

		private void UnassignService_Click(object sender, RoutedEventArgs e) =>
			SetSelectedAssignment(false);

		private void ReactivateService_Click(object sender, RoutedEventArgs e) =>
			SetSelectedAssignment(true);

		private void SetSelectedAssignment(bool isActive)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (dgAssignments.SelectedItem is not RoomServiceAssignmentDto selected)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(
					// Execute UI step inside SetSelectedAssignment
					"Vui lòng chọn dịch vụ trong tab 'Dịch vụ đã gán theo phòng'.",
					// Execute UI step inside SetSelectedAssignment
					"Chưa chọn dữ liệu",
					// Execute UI step inside SetSelectedAssignment
					MessageBoxButton.OK,
					// Execute UI step inside SetSelectedAssignment
					MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}
			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// RoomServiceAssignmentService.SetAssignment toggles room-service links
			if (ManagerUi.TryRun(() => _assignmentService.SetAssignment(managerId, selected.RoomId, selected.ServiceId, isActive)))
			{
				// Call helper LoadServices to refresh UI state from BLL data
				LoadServices();
			}
		}

		private void PopulateFilters()
		{
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbBuilding.ItemsSource = new[] { All }
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Concat(_services.Select(s => s.PropertyName).Distinct().OrderBy(name => name));
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbStatus.ItemsSource = new[] { All }
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Concat(_services.Select(s => s.StatusDisplay).Distinct().OrderBy(status => status));
			// Pick default combo index (usually first/all option) after reload
			cbBuilding.SelectedIndex = 0;
			// Pick default combo index (usually first/all option) after reload
			cbStatus.SelectedIndex = 0;
		}

		private void UpdateSummary()
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTotalServices.Text = _services.Count.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbActiveServices.Text = _services.Count(service => service.IsActive).ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbInactiveServices.Text = _services.Count(service => !service.IsActive).ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbMeteredServices.Text = _services.Count(service =>
				// Execute UI step inside UpdateSummary
				string.Equals(service.CalculationMethod, "Metered", StringComparison.OrdinalIgnoreCase)).ToString();
		}

		private bool FilterService(object item)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (item is not ServiceDto service)
			{
				// Exit method early or return value/tuple to caller
				return false;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			string keyword = txtSearch.Text.Trim();
			// Assign local/page state inside FilterService without altering business rules
			bool matchesKeyword = string.IsNullOrEmpty(keyword) ||
				// Execute UI step inside FilterService
				service.ServiceName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				// Execute UI step inside FilterService
				service.PropertyName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				// Execute UI step inside FilterService
				service.Unit.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			// Change combo selection to drive filter cascade or dialog default
			bool matchesBuilding = cbBuilding.SelectedItem is not string building ||
				// Assign local/page state inside FilterService without altering business rules
				building == All || service.PropertyName == building;
			// Change combo selection to drive filter cascade or dialog default
			bool matchesStatus = cbStatus.SelectedItem is not string status ||
				// Assign local/page state inside FilterService without altering business rules
				status == All || service.StatusDisplay == status;

			// Exit method early or return value/tuple to caller
			return matchesKeyword && matchesBuilding && matchesStatus;
		}

		// Re-run ICollectionView filter and update visible row count label
		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		private void RefreshView()
		{
			// Reapply ICollectionView filter after search or combo change
			_serviceView?.Refresh();
			// Aggregate list into KPI number shown on summary labels
			int count = _serviceView?.Cast<object>().Count() ?? 0;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbResultCount.Text = $"{count} dịch vụ";
			// Call helper SetState to refresh UI state from BLL data
			SetState(count == 0 ? "Không có dịch vụ phù hợp." : string.Empty, count == 0);
		}

		private void SetState(string message, bool isVisible)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbState.Text = message;
			// Show or hide panel/border for empty state or role-specific UI
			brdState.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
