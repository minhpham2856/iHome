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
	// quản lý dịch vụ — trái: catalog dịch vụ theo property; phải: phòng + gán dịch vụ theo building
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
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));

			// Create one EF Core context shared by services on this page
			_db = new IHomeDbContext();
			// Assign local/page state inside ServicesPage without altering business rules
			_service = new LandlordServiceService(_db);
			// Subscribe Unloaded to dispose shared DbContext when page leaves tree
			Unloaded += ServicesPage_Unloaded;

			// Execute UI step inside ServicesPage
			LoadPropertyFilter();
		}

		private void ServicesPage_Unloaded(object sender, RoutedEventArgs e)
		{
			// Unsubscribe Unloaded handler after single cleanup
			Unloaded -= ServicesPage_Unloaded;
			// Dispose shared IHomeDbContext to release SQL Server connection
			_db.Dispose();
		}

		private PropertyFilterOptionDto? SelectedProperty => cbProperty.SelectedItem as PropertyFilterOptionDto;
		private BuildingFilterOptionDto? SelectedBuilding => cbBuilding.SelectedItem as BuildingFilterOptionDto;
		private ServiceDto? SelectedService => dgServices.SelectedItem as ServiceDto;

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
				// Call helper LoadServices to refresh UI state from BLL data
				LoadServices();
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
				// Call helper ClearRooms to refresh UI state from BLL data
				ClearRooms("Chọn tòa để gán dịch vụ cho phòng.");
				// Enable/disable control during loading or when prerequisites missing
				btnAssign.IsEnabled = false;
				// Enable/disable control during loading or when prerequisites missing
				btnEditRoom.IsEnabled = false;
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = true;
				// Call page BLL service GetBuildingOptions to load or mutate scoped data
				var buildings = _service.GetBuildingOptions(_currentUser.Id, property.Id);
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

		private void LoadServices(int? keepServiceId = null)
		{
			// Assign local/page state inside LoadServices without altering business rules
			var property = SelectedProperty;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null)
			{
				// Execute UI step inside LoadServices
				ClearServices("Chọn nhà trọ để quản lý loại dịch vụ.");
				// Execute UI step inside LoadServices
				SetServiceToolbar(false);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetByProperty to load or mutate scoped data
				_services = _service.GetByProperty(_currentUser.Id, property.Id);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgServices.ItemsSource = _services;
				// Show or hide panel/border for empty state or role-specific UI
				brdServiceState.Visibility = _services.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				// Update TextBlock/TextBox caption or read user-entered text from control
				((TextBlock)brdServiceState.Child).Text = _services.Count == 0
					// Execute UI step inside LoadServices
					? "Chưa có dịch vụ. Bấm + Thêm để tạo."
					// Execute UI step inside LoadServices
					: string.Empty;
				// Show or hide panel/border for empty state or role-specific UI
				if (_services.Count > 0) brdServiceState.Visibility = Visibility.Collapsed;
				// Execute UI step inside LoadServices
				SetServiceToolbar(true);

				// Guard clause: only continue when UI selection, role, or input is valid
				if (keepServiceId.HasValue)
				{
					// Restore or set combo selection to match entity id or filter
					dgServices.SelectedItem = _services.FirstOrDefault(s => s.Id == keepServiceId.Value);
				}
				// Execute UI step inside LoadServices
				UpdateActionButtons();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Execute UI step inside LoadServices
				ClearServices("Không thể tải dịch vụ.");
				// Execute UI step inside LoadServices
				SetServiceToolbar(false);
			}
		}

		private void LoadRooms()
		{
			// Assign local/page state inside LoadRooms without altering business rules
			var building = SelectedBuilding;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (building == null)
			{
				// Call helper ClearRooms to refresh UI state from BLL data
				ClearRooms("Chọn tòa để gán dịch vụ cho phòng.");
				// Enable/disable control during loading or when prerequisites missing
				btnAssign.IsEnabled = false;
				// Enable/disable control during loading or when prerequisites missing
				btnEditRoom.IsEnabled = false;
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbRoomSection.Text = "Phòng & gán dịch vụ";
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetRoomsWithServices to load or mutate scoped data
				_rooms = _service.GetRoomsWithServices(_currentUser.Id, building.Id);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgRooms.ItemsSource = _rooms;
				// Show or hide panel/border for empty state or role-specific UI
				brdRoomState.Visibility = _rooms.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				// Update TextBlock/TextBox caption or read user-entered text from control
				((TextBlock)brdRoomState.Child).Text = _rooms.Count == 0
					// Execute UI step inside LoadRooms
					? "Tòa này chưa có phòng."
					// Execute UI step inside LoadRooms
					: string.Empty;
				// Show or hide panel/border for empty state or role-specific UI
				if (_rooms.Count > 0) brdRoomState.Visibility = Visibility.Collapsed;
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbRoomSection.Text = $"Phòng - {building.Name}";
				// Execute UI step inside LoadRooms
				UpdateActionButtons();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Call helper ClearRooms to refresh UI state from BLL data
				ClearRooms("Không thể tải phòng.");
				// Enable/disable control during loading or when prerequisites missing
				btnAssign.IsEnabled = false;
				// Enable/disable control during loading or when prerequisites missing
				btnEditRoom.IsEnabled = false;
			}
		}

		private void ClearServices(string message)
		{
			// Assign local/page state inside ClearServices without altering business rules
			_services = new();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgServices.ItemsSource = null;
			// Show or hide panel/border for empty state or role-specific UI
			brdServiceState.Visibility = Visibility.Visible;
			// Update TextBlock/TextBox caption or read user-entered text from control
			((TextBlock)brdServiceState.Child).Text = message;
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

		private void SetServiceToolbar(bool enabled)
		{
			// Enable/disable control during loading or when prerequisites missing
			btnAdd.IsEnabled = enabled;
			// Execute UI step inside SetServiceToolbar
			UpdateActionButtons();
		}

		// bật Gán/Sửa phòng khi có property + building + dịch vụ active + có phòng
		private void UpdateActionButtons()
		{
			// Assign local/page state inside UpdateActionButtons without altering business rules
			var selected = SelectedService;
			// Enable/disable control during loading or when prerequisites missing
			btnEdit.IsEnabled = selected != null;
			// Assign local/page state inside UpdateActionButtons without altering business rules
			bool canAssign = SelectedProperty != null
				// Assign local/page state inside UpdateActionButtons without altering business rules
				&& SelectedBuilding != null
				// Assign local/page state inside UpdateActionButtons without altering business rules
				&& _services.Any(s => s.IsActive)
				// Execute UI step inside UpdateActionButtons
				&& _rooms.Count > 0;
			// Enable/disable control during loading or when prerequisites missing
			btnAssign.IsEnabled = canAssign;
			// Enable/disable control during loading or when prerequisites missing
			btnEditRoom.IsEnabled = canAssign;
		}

		private void cbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_suppressFilterEvents) return;
			// Execute UI step inside cbProperty_SelectionChanged
			LoadBuildingFilter();
			// Call helper LoadServices to refresh UI state from BLL data
			LoadServices();
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

		private void btnAdd_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnAdd_Click without altering business rules
			var property = SelectedProperty;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null) return;

			// Call page BLL service GetCalculationMethodOptions to load or mutate scoped data
			var dialog = new ServiceDialog(property.Id, _service.GetCalculationMethodOptions())
			{
				// Resolve parent Window so modal dialogs center on the app shell
				Owner = Window.GetWindow(this)
			};
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
				try
				{
					// Call page BLL service Create to load or mutate scoped data
					_service.Create(_currentUser.Id, dialog.Result);
					// Call helper LoadServices to refresh UI state from BLL data
					LoadServices();
					// Execute UI step inside btnAdd_Click
					UpdateActionButtons();
				}
				// Catch expected validation, SMTP, or infrastructure failure for user feedback
				catch (Exception ex)
				{
					// Show Vietnamese MessageBox to confirm, warn, or report success/failure
					MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		// Assign local/page state inside btnEdit_Click without altering business rules
		private void btnEdit_Click(object sender, RoutedEventArgs e) => OpenEdit();

		private void dgServices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (FindVisualParent<DataGridColumnHeader>(e.OriginalSource as DependencyObject) != null) return;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedService != null) OpenEdit();
		}

		private void OpenEdit()
		{
			// Assign local/page state inside OpenEdit without altering business rules
			var selected = SelectedService;
			// Assign local/page state inside OpenEdit without altering business rules
			var property = SelectedProperty;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (selected == null || property == null) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetForm to load or mutate scoped data
				var form = _service.GetForm(_currentUser.Id, selected.Id);
				// Guard clause: only continue when UI selection, role, or input is valid
				if (form == null) return;

				// Call page BLL service GetCalculationMethodOptions to load or mutate scoped data
				var dialog = new ServiceDialog(property.Id, _service.GetCalculationMethodOptions(), form)
				{
					// Resolve parent Window so modal dialogs center on the app shell
					Owner = Window.GetWindow(this)
				};
				// Show modal dialog and block until user confirms or cancels
				if (dialog.ShowDialog() == true && dialog.Result != null)
				{
					// Call page BLL service Update to load or mutate scoped data
					_service.Update(_currentUser.Id, dialog.Result);
					// Call helper LoadServices to refresh UI state from BLL data
					LoadServices(dialog.Result.Id);
					// Call helper LoadRooms to refresh UI state from BLL data
					LoadRooms();
				}
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Assign local/page state inside btnEditRoom_Click without altering business rules
		private void btnEditRoom_Click(object sender, RoutedEventArgs e) => OpenEditRoomServices();

		private void dgRooms_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (FindVisualParent<DataGridColumnHeader>(e.OriginalSource as DependencyObject) != null) return;
			// Change combo selection to drive filter cascade or dialog default
			if (dgRooms.SelectedItem is RoomServiceSummaryDto) OpenEditRoomServices();
		}

		private void dgRooms_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
			UpdateActionButtons();

		// gán dịch vụ cho một phòng — EditRoomServicesDialog + SyncRoomAssignments
		private void OpenEditRoomServices()
		{
			// Assign local/page state inside OpenEditRoomServices without altering business rules
			var property = SelectedProperty;
			// Assign local/page state inside OpenEditRoomServices without altering business rules
			var building = SelectedBuilding;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null || building == null || _rooms.Count == 0) return;

			// Guard clause: only continue when UI selection, role, or input is valid
			if (!_services.Any(s => s.IsActive))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Chưa có dịch vụ đang hoạt động để gán.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Change combo selection to drive filter cascade or dialog default
			int? preferredRoomId = (dgRooms.SelectedItem as RoomServiceSummaryDto)?.RoomId;
			// Construct modal dialog prefilled with lookup lists or edit DTO
			var dialog = new EditRoomServicesDialog(
				// Execute UI step inside OpenEditRoomServices
				_rooms,
				// Execute UI step inside OpenEditRoomServices
				preferredRoomId,
				// Call page BLL service GetServiceOptionsForRoom to load or mutate scoped data
				roomId => _service.GetServiceOptionsForRoom(_currentUser.Id, roomId))
			{
				// Resolve parent Window so modal dialogs center on the app shell
				Owner = Window.GetWindow(this)
			};

			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true && dialog.RoomId.HasValue)
			{
				// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
				try
				{
					// Call page BLL service SyncRoomAssignments to load or mutate scoped data
					_service.SyncRoomAssignments(_currentUser.Id, dialog.RoomId.Value, dialog.SelectedServiceIds);
					// Call helper LoadServices to refresh UI state from BLL data
					LoadServices();
					// Call helper LoadRooms to refresh UI state from BLL data
					LoadRooms();
				}
				// Catch expected validation, SMTP, or infrastructure failure for user feedback
				catch (Exception ex)
				{
					// Show Vietnamese MessageBox to confirm, warn, or report success/failure
					MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		// gán hàng loạt — AssignServiceDialog chọn nhiều phòng + nhiều dịch vụ
		private void btnAssign_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnAssign_Click without altering business rules
			var property = SelectedProperty;
			// Assign local/page state inside btnAssign_Click without altering business rules
			var building = SelectedBuilding;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null || building == null) return;

			// Guard clause: only continue when UI selection, role, or input is valid
			if (_rooms.Count == 0)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Tòa này chưa có phòng để gán dịch vụ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Call page BLL service GetAssignServiceCheckOptions to load or mutate scoped data
			var serviceOptions = _service.GetAssignServiceCheckOptions(_currentUser.Id, property.Id);
			// Guard clause: only continue when UI selection, role, or input is valid
			if (serviceOptions.Count == 0)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Chưa có dịch vụ đang hoạt động để gán.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Change combo selection to drive filter cascade or dialog default
			int? preferredRoomId = (dgRooms.SelectedItem as RoomServiceSummaryDto)?.RoomId;
			// Call page BLL service GetAssignRoomCheckOptions to load or mutate scoped data
			var roomOptions = _service.GetAssignRoomCheckOptions(_currentUser.Id, building.Id, preferredRoomId);

			// Construct modal dialog prefilled with lookup lists or edit DTO
			var dialog = new AssignServiceDialog(roomOptions, serviceOptions)
			{
				// Resolve parent Window so modal dialogs center on the app shell
				Owner = Window.GetWindow(this)
			};

			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true)
			{
				// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
				try
				{
					// Call page BLL service SyncAssignmentsForRooms to load or mutate scoped data
					_service.SyncAssignmentsForRooms(
						// Execute UI step inside btnAssign_Click
						_currentUser.Id,
						// Execute UI step inside btnAssign_Click
						building.Id,
						// Execute UI step inside btnAssign_Click
						dialog.SelectedRoomIds,
						// Execute UI step inside btnAssign_Click
						dialog.SelectedServiceIds);
					// Call helper LoadServices to refresh UI state from BLL data
					LoadServices();
					// Call helper LoadRooms to refresh UI state from BLL data
					LoadRooms();
				}
				// Catch expected validation, SMTP, or infrastructure failure for user feedback
				catch (Exception ex)
				{
					// Show Vietnamese MessageBox to confirm, warn, or report success/failure
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
