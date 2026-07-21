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
	public partial class ServicesPage : Page
	{
		private readonly User _currentUser;
		private readonly IHomeDbContext _db;
		private readonly LandlordServiceService _service;
		private List<ServiceDto> _services = new();
		private List<RoomServiceSummaryDto> _rooms = new();
		private bool _suppressFilterEvents;

		public ServicesPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));

			_db = new IHomeDbContext();
			_service = new LandlordServiceService(_db);
			Unloaded += ServicesPage_Unloaded;

			LoadPropertyFilter();
		}

		private void ServicesPage_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= ServicesPage_Unloaded;
			_db.Dispose();
		}

		private PropertyFilterOptionDto? SelectedProperty => cboProperty.SelectedItem as PropertyFilterOptionDto;
		private BuildingFilterOptionDto? SelectedBuilding => cboBuilding.SelectedItem as BuildingFilterOptionDto;
		private ServiceDto? SelectedService => dgServices.SelectedItem as ServiceDto;

		private void LoadPropertyFilter(int? keepPropertyId = null, int? keepBuildingId = null)
		{
			try
			{
				_suppressFilterEvents = true;
				var properties = _service.GetPropertyOptions(_currentUser.Id);
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
				LoadServices();
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
				ClearRooms("Chọn tòa để gán dịch vụ cho phòng.");
				btnAssign.IsEnabled = false;
				btnEditRoom.IsEnabled = false;
				return;
			}

			try
			{
				_suppressFilterEvents = true;
				var buildings = _service.GetBuildingOptions(_currentUser.Id, property.Id);
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

				LoadRooms();
			}
			catch (Exception ex)
			{
				_suppressFilterEvents = false;
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void LoadServices(int? keepServiceId = null)
		{
			var property = SelectedProperty;
			if (property == null)
			{
				ClearServices("Chọn nhà trọ để quản lý loại dịch vụ.");
				SetServiceToolbar(false);
				return;
			}

			try
			{
				_services = _service.GetByProperty(_currentUser.Id, property.Id);
				dgServices.ItemsSource = _services;
				brdServiceState.Visibility = _services.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				((TextBlock)brdServiceState.Child).Text = _services.Count == 0
					? "Chưa có dịch vụ. Bấm + Thêm để tạo."
					: string.Empty;
				if (_services.Count > 0) brdServiceState.Visibility = Visibility.Collapsed;
				SetServiceToolbar(true);

				if (keepServiceId.HasValue)
				{
					dgServices.SelectedItem = _services.FirstOrDefault(s => s.Id == keepServiceId.Value);
				}
				UpdateActionButtons();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				ClearServices("Không thể tải dịch vụ.");
				SetServiceToolbar(false);
			}
		}

		private void LoadRooms()
		{
			var building = SelectedBuilding;
			if (building == null)
			{
				ClearRooms("Chọn tòa để gán dịch vụ cho phòng.");
				btnAssign.IsEnabled = false;
				btnEditRoom.IsEnabled = false;
				lblRoomSection.Text = "Phòng & gán dịch vụ";
				return;
			}

			try
			{
				_rooms = _service.GetRoomsWithServices(_currentUser.Id, building.Id);
				dgRooms.ItemsSource = _rooms;
				brdRoomState.Visibility = _rooms.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				((TextBlock)brdRoomState.Child).Text = _rooms.Count == 0
					? "Tòa này chưa có phòng."
					: string.Empty;
				if (_rooms.Count > 0) brdRoomState.Visibility = Visibility.Collapsed;
				lblRoomSection.Text = $"Phòng - {building.Name}";
				UpdateActionButtons();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				ClearRooms("Không thể tải phòng.");
				btnAssign.IsEnabled = false;
				btnEditRoom.IsEnabled = false;
			}
		}

		private void ClearServices(string message)
		{
			_services = new();
			dgServices.ItemsSource = null;
			brdServiceState.Visibility = Visibility.Visible;
			((TextBlock)brdServiceState.Child).Text = message;
		}

		private void ClearRooms(string message)
		{
			_rooms = new();
			dgRooms.ItemsSource = null;
			brdRoomState.Visibility = Visibility.Visible;
			((TextBlock)brdRoomState.Child).Text = message;
		}

		private void SetServiceToolbar(bool enabled)
		{
			btnAdd.IsEnabled = enabled;
			UpdateActionButtons();
		}

		private void UpdateActionButtons()
		{
			var selected = SelectedService;
			btnEdit.IsEnabled = selected != null;
			bool canAssign = SelectedProperty != null
				&& SelectedBuilding != null
				&& _services.Any(s => s.IsActive)
				&& _rooms.Count > 0;
			btnAssign.IsEnabled = canAssign;
			btnEditRoom.IsEnabled = canAssign;
		}

		private void cboProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressFilterEvents) return;
			LoadBuildingFilter();
			LoadServices();
		}

		private void cboBuilding_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressFilterEvents) return;
			LoadRooms();
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadPropertyFilter(SelectedProperty?.Id, SelectedBuilding?.Id);

		private void btnAdd_Click(object sender, RoutedEventArgs e)
		{
			var property = SelectedProperty;
			if (property == null) return;

			var dialog = new ServiceDialog(property.Id, _service.GetCalculationMethodOptions())
			{
				Owner = Window.GetWindow(this)
			};
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				try
				{
					_service.Create(_currentUser.Id, dialog.Result);
					LoadServices();
					UpdateActionButtons();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		private void btnEdit_Click(object sender, RoutedEventArgs e) => OpenEdit();

		private void dgServices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (FindVisualParent<DataGridColumnHeader>(e.OriginalSource as DependencyObject) != null) return;
			if (SelectedService != null) OpenEdit();
		}

		private void OpenEdit()
		{
			var selected = SelectedService;
			var property = SelectedProperty;
			if (selected == null || property == null) return;

			try
			{
				var form = _service.GetForm(_currentUser.Id, selected.Id);
				if (form == null) return;

				var dialog = new ServiceDialog(property.Id, _service.GetCalculationMethodOptions(), form)
				{
					Owner = Window.GetWindow(this)
				};
				if (dialog.ShowDialog() == true && dialog.Result != null)
				{
					_service.Update(_currentUser.Id, dialog.Result);
					LoadServices(dialog.Result.Id);
					LoadRooms();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEditRoom_Click(object sender, RoutedEventArgs e) => OpenEditRoomServices();

		private void dgRooms_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (FindVisualParent<DataGridColumnHeader>(e.OriginalSource as DependencyObject) != null) return;
			if (dgRooms.SelectedItem is RoomServiceSummaryDto) OpenEditRoomServices();
		}

		private void dgRooms_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
			UpdateActionButtons();

		private void OpenEditRoomServices()
		{
			var property = SelectedProperty;
			var building = SelectedBuilding;
			if (property == null || building == null || _rooms.Count == 0) return;

			if (!_services.Any(s => s.IsActive))
			{
				MessageBox.Show("Chưa có dịch vụ đang hoạt động để gán.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			int? preferredRoomId = (dgRooms.SelectedItem as RoomServiceSummaryDto)?.RoomId;
			var dialog = new EditRoomServicesDialog(
				_rooms,
				preferredRoomId,
				roomId => _service.GetServiceOptionsForRoom(_currentUser.Id, roomId))
			{
				Owner = Window.GetWindow(this)
			};

			if (dialog.ShowDialog() == true && dialog.RoomId.HasValue)
			{
				try
				{
					_service.SyncRoomAssignments(_currentUser.Id, dialog.RoomId.Value, dialog.SelectedServiceIds);
					LoadServices();
					LoadRooms();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		private void btnAssign_Click(object sender, RoutedEventArgs e)
		{
			var property = SelectedProperty;
			var building = SelectedBuilding;
			if (property == null || building == null) return;

			if (_rooms.Count == 0)
			{
				MessageBox.Show("Tòa này chưa có phòng để gán dịch vụ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var serviceOptions = _service.GetAssignServiceCheckOptions(_currentUser.Id, property.Id);
			if (serviceOptions.Count == 0)
			{
				MessageBox.Show("Chưa có dịch vụ đang hoạt động để gán.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			int? preferredRoomId = (dgRooms.SelectedItem as RoomServiceSummaryDto)?.RoomId;
			var roomOptions = _service.GetAssignRoomCheckOptions(_currentUser.Id, building.Id, preferredRoomId);

			var dialog = new AssignServiceDialog(roomOptions, serviceOptions)
			{
				Owner = Window.GetWindow(this)
			};

			if (dialog.ShowDialog() == true)
			{
				try
				{
					_service.SyncAssignmentsForRooms(
						_currentUser.Id,
						building.Id,
						dialog.SelectedRoomIds,
						dialog.SelectedServiceIds);
					LoadServices();
					LoadRooms();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		private void dgServices_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
			UpdateActionButtons();

		private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
		{
			while (child != null)
			{
				if (child is T parent) return parent;
				child = VisualTreeHelper.GetParent(child);
			}
			return null;
		}
	}
}
