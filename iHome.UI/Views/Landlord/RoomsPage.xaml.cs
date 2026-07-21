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
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));

			_db = new IHomeDbContext();
			_roomService = new LandlordRoomService(_db);
			_roomTypeService = new LandlordRoomTypeService(_db);
			Unloaded += RoomsPage_Unloaded;

			LoadPropertyFilter();
		}

		private void RoomsPage_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= RoomsPage_Unloaded;
			_db.Dispose();
		}

		private PropertyFilterOptionDto? SelectedProperty => cboProperty.SelectedItem as PropertyFilterOptionDto;
		private BuildingFilterOptionDto? SelectedBuilding => cboBuilding.SelectedItem as BuildingFilterOptionDto;
		private RoomTypeDto? SelectedRoomType => dgRoomTypes.SelectedItem as RoomTypeDto;
		private RoomDto? SelectedRoom => dgRooms.SelectedItem as RoomDto;

		private void LoadPropertyFilter(int? keepPropertyId = null, int? keepBuildingId = null)
		{
			try
			{
				_suppressFilterEvents = true;
				var properties = _roomService.GetPropertyOptions(_currentUser.Id);
				cboProperty.ItemsSource = properties;

				if (keepPropertyId.HasValue)
				{
					cboProperty.SelectedItem = properties.FirstOrDefault(p => p.Id == keepPropertyId.Value);
				}
				else if (properties.Count > 0)
				{
					cboProperty.SelectedIndex = 0;
				}
				else
				{
					cboProperty.SelectedIndex = -1;
				}
				_suppressFilterEvents = false;

				LoadBuildingFilter(keepBuildingId);
			}
			catch (Exception)
			{
				_suppressFilterEvents = false;
				MessageBox.Show("Không thể tải danh sách nhà trọ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void LoadBuildingFilter(int? keepBuildingId = null)
		{
			var property = SelectedProperty;
			if (property == null)
			{
				cboBuilding.ItemsSource = null;
				ClearRoomTypes("Chọn nhà trọ để quản lý loại phòng.");
				ClearRooms("Chọn tòa nhà để quản lý phòng.");
				SetRoomTypeToolbar(false);
				SetRoomToolbar(false);
				return;
			}

			try
			{
				_suppressFilterEvents = true;
				var buildings = _roomService.GetBuildingOptions(_currentUser.Id, property.Id);
				cboBuilding.ItemsSource = buildings;

				if (keepBuildingId.HasValue)
				{
					cboBuilding.SelectedItem = buildings.FirstOrDefault(b => b.Id == keepBuildingId.Value);
				}
				else if (buildings.Count > 0)
				{
					cboBuilding.SelectedIndex = 0;
				}
				else
				{
					cboBuilding.SelectedIndex = -1;
				}
				_suppressFilterEvents = false;

				LoadRoomTypes();
				LoadRooms();
			}
			catch (Exception ex)
			{
				_suppressFilterEvents = false;
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void LoadRoomTypes(int? keepRoomTypeId = null)
		{
			var property = SelectedProperty;
			if (property == null)
			{
				ClearRoomTypes("Chọn nhà trọ để quản lý loại phòng.");
				SetRoomTypeToolbar(false);
				return;
			}

			try
			{
				_roomTypes = _roomTypeService.GetByProperty(_currentUser.Id, property.Id);
				dgRoomTypes.ItemsSource = _roomTypes;
				brdRoomTypeState.Visibility = _roomTypes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				((TextBlock)brdRoomTypeState.Child).Text = _roomTypes.Count == 0
					? "Chưa có loại phòng. Bấm + Thêm để tạo."
					: string.Empty;
				if (_roomTypes.Count > 0) brdRoomTypeState.Visibility = Visibility.Collapsed;
				SetRoomTypeToolbar(true);

				if (keepRoomTypeId.HasValue)
				{
					dgRoomTypes.SelectedItem = _roomTypes.FirstOrDefault(rt => rt.Id == keepRoomTypeId.Value);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				ClearRoomTypes("Không thể tải loại phòng.");
				SetRoomTypeToolbar(false);
			}
		}

		private void LoadRooms(int? keepRoomId = null)
		{
			var building = SelectedBuilding;
			if (building == null)
			{
				ClearRooms("Chọn tòa nhà để xem và quản lý phòng.");
				SetRoomToolbar(false);
				lblRoomSection.Text = "Phòng";
				return;
			}

			try
			{
				_rooms = _roomService.GetByBuilding(_currentUser.Id, building.Id);
				dgRooms.ItemsSource = _rooms;
				brdRoomState.Visibility = _rooms.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				((TextBlock)brdRoomState.Child).Text = _rooms.Count == 0
					? "Chưa có phòng. Bấm + Thêm phòng để tạo."
					: string.Empty;
				if (_rooms.Count > 0) brdRoomState.Visibility = Visibility.Collapsed;
				lblRoomSection.Text = $"Phòng - {building.Name}";
				SetRoomToolbar(true);

				if (keepRoomId.HasValue)
				{
					dgRooms.SelectedItem = _rooms.FirstOrDefault(r => r.Id == keepRoomId.Value);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				ClearRooms("Không thể tải phòng.");
				SetRoomToolbar(false);
			}
		}

		private void ClearRoomTypes(string message)
		{
			_roomTypes = new();
			dgRoomTypes.ItemsSource = null;
			brdRoomTypeState.Visibility = Visibility.Visible;
			((TextBlock)brdRoomTypeState.Child).Text = message;
		}

		private void ClearRooms(string message)
		{
			_rooms = new();
			dgRooms.ItemsSource = null;
			brdRoomState.Visibility = Visibility.Visible;
			((TextBlock)brdRoomState.Child).Text = message;
		}

		private void SetRoomTypeToolbar(bool enabled)
		{
			btnAddRoomType.IsEnabled = enabled;
			btnEditRoomType.IsEnabled = enabled;
			btnDeleteRoomType.IsEnabled = enabled;
		}

		private void SetRoomToolbar(bool enabled)
		{
			btnAddRoom.IsEnabled = enabled;
			btnEditRoom.IsEnabled = enabled;
			btnViewRoom.IsEnabled = enabled;
			btnDeleteRoom.IsEnabled = enabled;
		}

		private static bool IsRowDoubleClick(MouseButtonEventArgs e)
		{
			DependencyObject? current = e.OriginalSource as DependencyObject;
			while (current != null)
			{
				if (current is DataGridColumnHeader) return false;
				if (current is DataGridRow) return true;
				current = VisualTreeHelper.GetParent(current);
			}
			return false;
		}

		private void cboProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressFilterEvents) return;
			LoadBuildingFilter();
		}

		private void cboBuilding_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressFilterEvents) return;
			LoadRooms();
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadPropertyFilter(SelectedProperty?.Id, SelectedBuilding?.Id);

		private void btnAddRoomType_Click(object sender, RoutedEventArgs e)
		{
			var property = SelectedProperty;
			if (property == null) return;

			var dialog = new RoomTypeDialog(property.Id) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() != true || dialog.Result == null) return;

			try
			{
				_roomTypeService.Create(_currentUser.Id, dialog.Result);
				LoadRoomTypes();
				MessageBox.Show("Đã thêm loại phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEditRoomType_Click(object sender, RoutedEventArgs e)
		{
			var property = SelectedProperty;
			var selected = SelectedRoomType;
			if (property == null || selected == null)
			{
				MessageBox.Show("Vui lòng chọn loại phòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			try
			{
				var form = _roomTypeService.GetForm(_currentUser.Id, selected.Id);
				var dialog = new RoomTypeDialog(property.Id, form) { Owner = Window.GetWindow(this) };
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				_roomTypeService.Update(_currentUser.Id, dialog.Result);
				LoadRoomTypes(selected.Id);
				LoadRooms(SelectedRoom?.Id);
				MessageBox.Show("Đã cập nhật loại phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnDeleteRoomType_Click(object sender, RoutedEventArgs e)
		{
			var selected = SelectedRoomType;
			if (selected == null)
			{
				MessageBox.Show("Vui lòng chọn loại phòng cần xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var confirm = MessageBox.Show(
				$"Xóa loại phòng \"{selected.TypeName}\"?",
				"Xác nhận",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			if (confirm != MessageBoxResult.Yes) return;

			try
			{
				_roomTypeService.Delete(_currentUser.Id, selected.Id);
				LoadRoomTypes();
				MessageBox.Show("Đã xóa loại phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnAddRoom_Click(object sender, RoutedEventArgs e)
		{
			var property = SelectedProperty;
			var building = SelectedBuilding;
			if (property == null || building == null) return;

			try
			{
				var types = _roomService.GetRoomTypeOptions(_currentUser.Id, property.Id);
				if (types.Count == 0)
				{
					MessageBox.Show("Hãy tạo loại phòng cho nhà trọ trước khi thêm phòng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				var dialog = new RoomDialog(building.Id, types) { Owner = Window.GetWindow(this) };
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				_roomService.Create(_currentUser.Id, dialog.Result);
				LoadRoomTypes();
				LoadRooms();
				MessageBox.Show("Đã thêm phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEditRoom_Click(object sender, RoutedEventArgs e)
		{
			var property = SelectedProperty;
			var building = SelectedBuilding;
			var selected = SelectedRoom;
			if (property == null || building == null || selected == null)
			{
				MessageBox.Show("Vui lòng chọn phòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			try
			{
				var form = _roomService.GetForm(_currentUser.Id, selected.Id);
				var types = _roomService.GetRoomTypeOptions(_currentUser.Id, property.Id);
				bool hasOccupants = selected.CurrentOccupancy > 0;
				var dialog = new RoomDialog(building.Id, types, form, hasOccupants) { Owner = Window.GetWindow(this) };
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				_roomService.Update(_currentUser.Id, dialog.Result);
				LoadRoomTypes();
				LoadRooms(selected.Id);
				MessageBox.Show("Đã cập nhật phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnViewRoom_Click(object sender, RoutedEventArgs e)
		{
			var selected = SelectedRoom;
			if (selected == null)
			{
				MessageBox.Show("Vui lòng chọn phòng cần xem.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			try
			{
				var detail = _roomService.GetDetail(_currentUser.Id, selected.Id);
				var dialog = new RoomDetailDialog(detail) { Owner = Window.GetWindow(this) };
				dialog.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnDeleteRoom_Click(object sender, RoutedEventArgs e)
		{
			var selected = SelectedRoom;
			if (selected == null)
			{
				MessageBox.Show("Vui lòng chọn phòng cần xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var confirm = MessageBox.Show(
				$"Xóa phòng \"{selected.RoomNumber}\"?",
				"Xác nhận",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			if (confirm != MessageBoxResult.Yes) return;

			try
			{
				_roomService.Delete(_currentUser.Id, selected.Id);
				LoadRoomTypes();
				LoadRooms();
				MessageBox.Show("Đã xóa phòng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void dgRoomTypes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!IsRowDoubleClick(e)) return;
			if (SelectedRoomType != null) btnEditRoomType_Click(sender, e);
		}

		private void dgRooms_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!IsRowDoubleClick(e)) return;
			if (SelectedRoom != null) btnViewRoom_Click(sender, e);
		}
	}
}
