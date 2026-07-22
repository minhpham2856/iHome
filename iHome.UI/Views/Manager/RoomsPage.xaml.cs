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
	public partial class RoomsPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _buildingId;
		private readonly BLL.Services.Manager.RoomService _service = new();
		private List<RoomDto> _rooms = new();
		private ICollectionView? _roomView;
		private bool _isLoading;

		public RoomsPage(User currentUser, int? buildingId)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside RoomsPage without altering business rules
			_currentUser = currentUser;
			// Assign local/page state inside RoomsPage without altering business rules
			_buildingId = buildingId;
			// Subscribe page Loaded event to defer BLL calls until controls exist
			Loaded += RoomsPage_Loaded;
		}

		private void RoomsPage_Loaded(object sender, RoutedEventArgs e) =>
			LoadRooms();

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadRooms();

		private void LoadRooms()
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
				SetState("Đang tải danh sách phòng...", true);
				// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				// Call page BLL service GetRooms to load or mutate scoped data
				_rooms = _service.GetRooms(managerId, _buildingId);

				// Wrap list in ICollectionView to enable client-side Filter
				_roomView = CollectionViewSource.GetDefaultView(_rooms);
				// Attach filter predicate so grid hides rows not matching search/combos
				_roomView.Filter = FilterRoom;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgRooms.ItemsSource = _roomView;
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
				SetState("Không thể tải danh sách phòng. Vui lòng thử lại.", true);
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
				.Concat(_rooms.Select(r => r.BuildingName).Distinct().OrderBy(name => name));
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbStatus.ItemsSource = new[] { All }
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Concat(_rooms.Select(r => r.StatusDisplay).Distinct().OrderBy(status => status));
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbFloor.ItemsSource = new[] { All }
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Concat(_rooms.Select(r => r.Floor.ToString()).Distinct().OrderBy(floor => floor));
			// Pick default combo index (usually first/all option) after reload
			cbBuilding.SelectedIndex = 0;
			// Pick default combo index (usually first/all option) after reload
			cbStatus.SelectedIndex = 0;
			// Pick default combo index (usually first/all option) after reload
			cbFloor.SelectedIndex = 0;
		}

		private void UpdateSummary()
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTotalRooms.Text = _rooms.Count.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbOccupiedRooms.Text = _rooms.Count(room =>
				// Execute UI step inside UpdateSummary
				string.Equals(room.Status, "Occupied", StringComparison.OrdinalIgnoreCase)).ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbVacantRooms.Text = _rooms.Count(room =>
				// Execute UI step inside UpdateSummary
				string.Equals(room.Status, "Vacant", StringComparison.OrdinalIgnoreCase) ||
				// Execute UI step inside UpdateSummary
				string.Equals(room.Status, "Empty", StringComparison.OrdinalIgnoreCase)).ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbMaintenanceRooms.Text = _rooms.Count(room =>
				// Execute UI step inside UpdateSummary
				string.Equals(room.Status, "Maintenance", StringComparison.OrdinalIgnoreCase)).ToString();
		}

		private bool FilterRoom(object item)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (item is not RoomDto room)
			{
				// Exit method early or return value/tuple to caller
				return false;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			string keyword = txtSearch.Text.Trim();
			// Assign local/page state inside FilterRoom without altering business rules
			bool matchesKeyword = string.IsNullOrEmpty(keyword) ||
				// Execute UI step inside FilterRoom
				room.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				// Execute UI step inside FilterRoom
				room.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				// Execute UI step inside FilterRoom
				room.RoomTypeName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			// Change combo selection to drive filter cascade or dialog default
			bool matchesBuilding = cbBuilding.SelectedItem is not string building ||
				// Assign local/page state inside FilterRoom without altering business rules
				building == All || room.BuildingName == building;
			// Change combo selection to drive filter cascade or dialog default
			bool matchesStatus = cbStatus.SelectedItem is not string status ||
				// Assign local/page state inside FilterRoom without altering business rules
				status == All || room.StatusDisplay == status;
			// Change combo selection to drive filter cascade or dialog default
			bool matchesFloor = cbFloor.SelectedItem is not string floor ||
				// Assign local/page state inside FilterRoom without altering business rules
				floor == All || room.Floor.ToString() == floor;

			// Exit method early or return value/tuple to caller
			return matchesKeyword && matchesBuilding && matchesStatus && matchesFloor;
		}

		// Re-run ICollectionView filter and update visible row count label
		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		private void RefreshView()
		{
			// Reapply ICollectionView filter after search or combo change
			_roomView?.Refresh();
			// Aggregate list into KPI number shown on summary labels
			int count = _roomView?.Cast<object>().Count() ?? 0;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbResultCount.Text = $"{count} phòng";
			// Call helper SetState to refresh UI state from BLL data
			SetState(count == 0 ? "Không có phòng phù hợp." : string.Empty, count == 0);
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
