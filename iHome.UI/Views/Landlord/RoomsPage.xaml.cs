using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Services;
using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace iHome.UI.Views.Landlord
{
	// quản lý phòng — property/building filter; trái: loại phòng; phải: phòng trong tòa; CRUD + xem chi tiết
	public partial class RoomsPage : Page
	{
		private readonly User _currentUser;
		private readonly IHomeDbContext _db;
		private readonly LandlordRoomService _roomService;
		private readonly LandlordRoomTypeService _roomTypeService;
		private List<RoomTypeDto> _roomTypes = new();
		private List<RoomDto> _rooms = new();
		private bool _suppressFilterEvents;

		public RoomsPage(User user)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));

			// Create one EF Core context shared by services on this page
			_db = new IHomeDbContext();
			// Assign local/page state inside RoomsPage without altering business rules
			_roomService = new LandlordRoomService(_db);
			// Assign local/page state inside RoomsPage without altering business rules
			_roomTypeService = new LandlordRoomTypeService(_db);
			// Subscribe Unloaded to dispose shared DbContext when page leaves tree
			Unloaded += RoomsPage_Unloaded;

			// Execute UI step inside RoomsPage
			LoadPropertyFilter();
		}

		private void RoomsPage_Unloaded(object sender, RoutedEventArgs e)
		{
			// Unsubscribe Unloaded handler after single cleanup
			Unloaded -= RoomsPage_Unloaded;
			// Dispose shared IHomeDbContext to release SQL Server connection
			_db.Dispose();
		}

		private PropertyFilterOptionDto? SelectedProperty => cbProperty.SelectedItem as PropertyFilterOptionDto;
		private BuildingFilterOptionDto? SelectedBuilding => cbBuilding.SelectedItem as BuildingFilterOptionDto;
		private RoomTypeDto? SelectedRoomType => dgRoomTypes.SelectedItem as RoomTypeDto;
		private RoomDto? SelectedRoom => dgRooms.SelectedItem as RoomDto;

		private void LoadPropertyFilter(int? keepPropertyId = null, int? keepBuildingId = null)
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = true;
				// LandlordRoomService.GetPropertyOptions manages rooms and filter dropdown options
				var properties = _roomService.GetPropertyOptions(_currentUser.Id);
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
				// Alternate branch when previous condition was not satisfied
				else
				{
					// Pick default combo index (usually first/all option) after reload
					cbProperty.SelectedIndex = -1;
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
			var property = SelectedProperty;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null)
			{
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				cbBuilding.ItemsSource = null;
				// Execute UI step inside LoadBuildingFilter
				ClearRoomTypes("Chọn nhà trọ để quản lý loại phòng.");
				// Call helper ClearRooms to refresh UI state from BLL data
				ClearRooms("Chọn tòa nhà để quản lý phòng.");
				// Execute UI step inside LoadBuildingFilter
				SetRoomTypeToolbar(false);
				// Execute UI step inside LoadBuildingFilter
				SetRoomToolbar(false);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = true;
				// LandlordRoomService.GetBuildingOptions manages rooms and filter dropdown options
				var buildings = _roomService.GetBuildingOptions(_currentUser.Id, property.Id);
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
				// Alternate branch when previous condition was not satisfied
				else
				{
					// Pick default combo index (usually first/all option) after reload
					cbBuilding.SelectedIndex = -1;
				}
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = false;

				// Execute UI step inside LoadBuildingFilter
				LoadRoomTypes();
				// Call helper LoadRooms to refresh UI state from BLL data
				LoadRooms();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = false;
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// loại phòng scope theo property (không theo building)
		private void LoadRoomTypes(int? keepRoomTypeId = null)
		{
			// Assign local/page state inside LoadRoomTypes without altering business rules
			var property = SelectedProperty;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null)
			{
				// Execute UI step inside LoadRoomTypes
				ClearRoomTypes("Chọn nhà trọ để quản lý loại phòng.");
				// Execute UI step inside LoadRoomTypes
				SetRoomTypeToolbar(false);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordRoomTypeService.GetByProperty manages room types for selected property
				_roomTypes = _roomTypeService.GetByProperty(_currentUser.Id, property.Id);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgRoomTypes.ItemsSource = _roomTypes;
				// Show or hide panel/border for empty state or role-specific UI
				brdRoomTypeState.Visibility = _roomTypes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				// Update TextBlock/TextBox caption or read user-entered text from control
				((TextBlock)brdRoomTypeState.Child).Text = _roomTypes.Count == 0
					// Execute UI step inside LoadRoomTypes
					? "Chưa có loại phòng. Bấm + Thêm để tạo."
					// Execute UI step inside LoadRoomTypes
					: string.Empty;
				// Show or hide panel/border for empty state or role-specific UI
				if (_roomTypes.Count > 0) brdRoomTypeState.Visibility = Visibility.Collapsed;
				// Execute UI step inside LoadRoomTypes
				SetRoomTypeToolbar(true);

				// Guard clause: only continue when UI selection, role, or input is valid
				if (keepRoomTypeId.HasValue)
				{
					// Restore or set combo selection to match entity id or filter
					dgRoomTypes.SelectedItem = _roomTypes.FirstOrDefault(rt => rt.Id == keepRoomTypeId.Value);
				}
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Execute UI step inside LoadRoomTypes
				ClearRoomTypes("Không thể tải loại phòng.");
				// Execute UI step inside LoadRoomTypes
				SetRoomTypeToolbar(false);
			}
		}

		// phòng scope theo building đang chọn
		private void LoadRooms(int? keepRoomId = null)
		{
			// Assign local/page state inside LoadRooms without altering business rules
			var building = SelectedBuilding;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (building == null)
			{
				// Call helper ClearRooms to refresh UI state from BLL data
				ClearRooms("Chọn tòa nhà để xem và quản lý phòng.");
				// Execute UI step inside LoadRooms
				SetRoomToolbar(false);
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbRoomSection.Text = "Phòng";
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordRoomService.GetByBuilding manages rooms and filter dropdown options
				_rooms = _roomService.GetByBuilding(_currentUser.Id, building.Id);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgRooms.ItemsSource = _rooms;
				// Show or hide panel/border for empty state or role-specific UI
				brdRoomState.Visibility = _rooms.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				// Update TextBlock/TextBox caption or read user-entered text from control
				((TextBlock)brdRoomState.Child).Text = _rooms.Count == 0
					// Execute UI step inside LoadRooms
					? "Chưa có phòng. Bấm + Thêm phòng để tạo."
					// Execute UI step inside LoadRooms
					: string.Empty;
				// Show or hide panel/border for empty state or role-specific UI
				if (_rooms.Count > 0) brdRoomState.Visibility = Visibility.Collapsed;
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbRoomSection.Text = $"Phòng - {building.Name}";
				// Execute UI step inside LoadRooms
				SetRoomToolbar(true);

				// Guard clause: only continue when UI selection, role, or input is valid
				if (keepRoomId.HasValue)
				{
					// Restore or set combo selection to match entity id or filter
					dgRooms.SelectedItem = _rooms.FirstOrDefault(r => r.Id == keepRoomId.Value);
				}
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Call helper ClearRooms to refresh UI state from BLL data
				ClearRooms("Không thể tải phòng.");
				// Execute UI step inside LoadRooms
				SetRoomToolbar(false);
			}
		}

		private void ClearRoomTypes(string message)
		{
			// Assign local/page state inside ClearRoomTypes without altering business rules
			_roomTypes = new();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgRoomTypes.ItemsSource = null;
			// Show or hide panel/border for empty state or role-specific UI
			brdRoomTypeState.Visibility = Visibility.Visible;
			// Update TextBlock/TextBox caption or read user-entered text from control
			((TextBlock)brdRoomTypeState.Child).Text = message;
		}

		private void ClearRooms(string message)
		{
			// Assign local/page state inside ClearRooms without altering business rules
			_rooms = new();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgRooms.ItemsSource = null;
			// Show or hide panel/border for empty state or role-specific UI
			brdRoomState.Visibility = Visibility.Visible;
			// Update TextBlock/TextBox caption or read user-entered text from control
			((TextBlock)brdRoomState.Child).Text = message;
		}

		private void SetRoomTypeToolbar(bool enabled)
		{
			// Enable/disable control during loading or when prerequisites missing
			btnAddRoomType.IsEnabled = enabled;
			// Enable/disable control during loading or when prerequisites missing
			btnEditRoomType.IsEnabled = enabled;
			// Enable/disable control during loading or when prerequisites missing
			btnDeleteRoomType.IsEnabled = enabled;
		}

		private void SetRoomToolbar(bool enabled)
		{
			// Enable/disable control during loading or when prerequisites missing
			btnAddRoom.IsEnabled = enabled;
			// Enable/disable control during loading or when prerequisites missing
			btnEditRoom.IsEnabled = enabled;
			// Enable/disable control during loading or when prerequisites missing
			btnViewRoom.IsEnabled = enabled;
			// Enable/disable control during loading or when prerequisites missing
			btnDeleteRoom.IsEnabled = enabled;
		}

		// only treat double-click on a data row as action; header clicks stay for sort
		private static bool IsRowDoubleClick(MouseButtonEventArgs e)
		{
			// Assign local/page state inside IsRowDoubleClick without altering business rules
			DependencyObject? current = e.OriginalSource as DependencyObject;
			// Assign local/page state inside IsRowDoubleClick without altering business rules
			while (current != null)
			{
				// Guard clause: only continue when UI selection, role, or input is valid
				if (current is DataGridColumnHeader) return false;
				// Guard clause: only continue when UI selection, role, or input is valid
				if (current is DataGridRow) return true;
				// Assign local/page state inside IsRowDoubleClick without altering business rules
				current = VisualTreeHelper.GetParent(current);
			}
			// Exit method early or return value/tuple to caller
			return false;
		}

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
			// Call helper LoadRooms to refresh UI state from BLL data
			LoadRooms();
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadPropertyFilter(SelectedProperty?.Id, SelectedBuilding?.Id);

		private void btnAddRoomType_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnAddRoomType_Click without altering business rules
			var property = SelectedProperty;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null) return;

			// Resolve parent Window so modal dialogs center on the app shell
			var dialog = new RoomTypeDialog(property.Id) { Owner = Window.GetWindow(this) };
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() != true || dialog.Result == null) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordRoomTypeService.Create manages room types for selected property
				_roomTypeService.Create(_currentUser.Id, dialog.Result);
				// Execute UI step inside btnAddRoomType_Click
				LoadRoomTypes();
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã thêm loại phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEditRoomType_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnEditRoomType_Click without altering business rules
			var property = SelectedProperty;
			// Assign local/page state inside btnEditRoomType_Click without altering business rules
			var selected = SelectedRoomType;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null || selected == null)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn loại phòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordRoomTypeService.GetForm manages room types for selected property
				var form = _roomTypeService.GetForm(_currentUser.Id, selected.Id);
				// Resolve parent Window so modal dialogs center on the app shell
				var dialog = new RoomTypeDialog(property.Id, form) { Owner = Window.GetWindow(this) };
				// Show modal dialog and block until user confirms or cancels
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				// LandlordRoomTypeService.Update manages room types for selected property
				_roomTypeService.Update(_currentUser.Id, dialog.Result);
				// Execute UI step inside btnEditRoomType_Click
				LoadRoomTypes(selected.Id);
				// Call helper LoadRooms to refresh UI state from BLL data
				LoadRooms(SelectedRoom?.Id);
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã cập nhật loại phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnDeleteRoomType_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnDeleteRoomType_Click without altering business rules
			var selected = SelectedRoomType;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (selected == null)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn loại phòng cần xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Show Vietnamese MessageBox to confirm, warn, or report success/failure
			var confirm = MessageBox.Show(
				// Execute UI step inside btnDeleteRoomType_Click
				$"Xóa loại phòng \"{selected.TypeName}\"?",
				// Execute UI step inside btnDeleteRoomType_Click
				"Xác nhận",
				// Execute UI step inside btnDeleteRoomType_Click
				MessageBoxButton.YesNo,
				// Execute UI step inside btnDeleteRoomType_Click
				MessageBoxImage.Question);
			// Guard clause: only continue when UI selection, role, or input is valid
			if (confirm != MessageBoxResult.Yes) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordRoomTypeService.Delete manages room types for selected property
				_roomTypeService.Delete(_currentUser.Id, selected.Id);
				// Execute UI step inside btnDeleteRoomType_Click
				LoadRoomTypes();
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã xóa loại phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnAddRoom_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnAddRoom_Click without altering business rules
			var property = SelectedProperty;
			// Assign local/page state inside btnAddRoom_Click without altering business rules
			var building = SelectedBuilding;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null || building == null) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordRoomService.GetRoomTypeOptions manages rooms and filter dropdown options
				var types = _roomService.GetRoomTypeOptions(_currentUser.Id, property.Id);
				// Guard clause: only continue when UI selection, role, or input is valid
				if (types.Count == 0)
				{
					// Show Vietnamese MessageBox to confirm, warn, or report success/failure
					MessageBox.Show("Hãy tạo loại phòng cho nhà trọ trước khi thêm phòng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
					// Exit method early or return value/tuple to caller
					return;
				}

				// Resolve parent Window so modal dialogs center on the app shell
				var dialog = new RoomDialog(building.Id, types) { Owner = Window.GetWindow(this) };
				// Show modal dialog and block until user confirms or cancels
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				// LandlordRoomService.Create manages rooms and filter dropdown options
				_roomService.Create(_currentUser.Id, dialog.Result);
				// Execute UI step inside btnAddRoom_Click
				LoadRoomTypes();
				// Call helper LoadRooms to refresh UI state from BLL data
				LoadRooms();
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã thêm phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEditRoom_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnEditRoom_Click without altering business rules
			var property = SelectedProperty;
			// Assign local/page state inside btnEditRoom_Click without altering business rules
			var building = SelectedBuilding;
			// Assign local/page state inside btnEditRoom_Click without altering business rules
			var selected = SelectedRoom;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null || building == null || selected == null)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn phòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordRoomService.GetForm manages rooms and filter dropdown options
				var form = _roomService.GetForm(_currentUser.Id, selected.Id);
				// LandlordRoomService.GetRoomTypeOptions manages rooms and filter dropdown options
				var types = _roomService.GetRoomTypeOptions(_currentUser.Id, property.Id);
				// Assign local/page state inside btnEditRoom_Click without altering business rules
				bool hasOccupants = selected.CurrentOccupancy > 0;
				// Resolve parent Window so modal dialogs center on the app shell
				var dialog = new RoomDialog(building.Id, types, form, hasOccupants) { Owner = Window.GetWindow(this) };
				// Show modal dialog and block until user confirms or cancels
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				// LandlordRoomService.Update manages rooms and filter dropdown options
				_roomService.Update(_currentUser.Id, dialog.Result);
				// Execute UI step inside btnEditRoom_Click
				LoadRoomTypes();
				// Call helper LoadRooms to refresh UI state from BLL data
				LoadRooms(selected.Id);
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã cập nhật phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// mở RoomDetailDialog read-only — double-click row cũng gọi action này
		private void btnViewRoom_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnViewRoom_Click without altering business rules
			var selected = SelectedRoom;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (selected == null)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn phòng cần xem.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordRoomService.GetDetail manages rooms and filter dropdown options
				var detail = _roomService.GetDetail(_currentUser.Id, selected.Id);
				// Resolve parent Window so modal dialogs center on the app shell
				var dialog = new RoomDetailDialog(detail) { Owner = Window.GetWindow(this) };
				// Show modal dialog and block until user confirms or cancels
				dialog.ShowDialog();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnDeleteRoom_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnDeleteRoom_Click without altering business rules
			var selected = SelectedRoom;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (selected == null)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn phòng cần xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Show Vietnamese MessageBox to confirm, warn, or report success/failure
			var confirm = MessageBox.Show(
				// Execute UI step inside btnDeleteRoom_Click
				$"Xóa phòng \"{selected.RoomNumber}\"?",
				// Execute UI step inside btnDeleteRoom_Click
				"Xác nhận",
				// Execute UI step inside btnDeleteRoom_Click
				MessageBoxButton.YesNo,
				// Execute UI step inside btnDeleteRoom_Click
				MessageBoxImage.Question);
			// Guard clause: only continue when UI selection, role, or input is valid
			if (confirm != MessageBoxResult.Yes) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordRoomService.Delete manages rooms and filter dropdown options
				_roomService.Delete(_currentUser.Id, selected.Id);
				// Execute UI step inside btnDeleteRoom_Click
				LoadRoomTypes();
				// Call helper LoadRooms to refresh UI state from BLL data
				LoadRooms();
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã xóa phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void dgRoomTypes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Ignore grid header double-clicks; only data rows open edit/view
			if (!IsRowDoubleClick(e)) return;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedRoomType != null) btnEditRoomType_Click(sender, e);
		}

		private void dgRooms_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Ignore grid header double-clicks; only data rows open edit/view
			if (!IsRowDoubleClick(e)) return;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedRoom != null) btnViewRoom_Click(sender, e);
		}
	}
}
