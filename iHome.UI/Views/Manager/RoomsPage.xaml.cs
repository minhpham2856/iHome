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
	// Trang danh sách phòng: lọc và xem tóm tắt trạng thái
	public partial class RoomsPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _buildingId;
		private readonly BLL.Services.Manager.RoomService _service = new();
		private List<RoomDto> _rooms = new();
		private ICollectionView? _roomView;
		private bool _isLoading;

		// Khởi tạo theo user và tòa nhà đang chọn
		public RoomsPage(User currentUser, int? buildingId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_buildingId = buildingId;
			Loaded += RoomsPage_Loaded;
		}

		// Nạp danh sách khi trang sẵn sàng
		private void RoomsPage_Loaded(object sender, RoutedEventArgs e) =>
			LoadRooms();

		// Làm mới danh sách phòng
		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadRooms();

		// Tải phòng và gắn bộ lọc
		private void LoadRooms()
		{
			if (_isLoading)
			{
				return;
			}

			try
			{
				_isLoading = true;
				btnRefresh.IsEnabled = false;
				SetState("Đang tải danh sách phòng...", true);
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				_rooms = _service.GetRooms(managerId, _buildingId);

				_roomView = CollectionViewSource.GetDefaultView(_rooms);
				_roomView.Filter = FilterRoom;
				dgRooms.ItemsSource = _roomView;
				PopulateFilters();
				UpdateSummary();
				RefreshView();
			}
			catch (Exception)
			{
				SetState("Không thể tải danh sách phòng. Vui lòng thử lại.", true);
			}
			finally
			{
				_isLoading = false;
				btnRefresh.IsEnabled = true;
			}
		}

		// Nạp combo lọc tòa / trạng thái / tầng
		private void PopulateFilters()
		{
			cbBuilding.ItemsSource = new[] { All }
				.Concat(_rooms.Select(r => r.BuildingName).Distinct().OrderBy(name => name));
			cbStatus.ItemsSource = new[] { All }
				.Concat(_rooms.Select(r => r.StatusDisplay).Distinct().OrderBy(status => status));
			cbFloor.ItemsSource = new[] { All }
				.Concat(_rooms.Select(r => r.Floor.ToString()).Distinct().OrderBy(floor => floor));
			cbBuilding.SelectedIndex = 0;
			cbStatus.SelectedIndex = 0;
			cbFloor.SelectedIndex = 0;
		}

		// Cập nhật KPI tổng / đang thuê / trống / bảo trì
		private void UpdateSummary()
		{
			lbTotalRooms.Text = _rooms.Count.ToString();
			lbOccupiedRooms.Text = _rooms.Count(room =>
				string.Equals(room.Status, "Occupied", StringComparison.OrdinalIgnoreCase)).ToString();
			lbVacantRooms.Text = _rooms.Count(room =>
				string.Equals(room.Status, "Vacant", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(room.Status, "Empty", StringComparison.OrdinalIgnoreCase)).ToString();
			lbMaintenanceRooms.Text = _rooms.Count(room =>
				string.Equals(room.Status, "Maintenance", StringComparison.OrdinalIgnoreCase)).ToString();
		}

		// Điều kiện lọc theo từ khóa và combo
		private bool FilterRoom(object item)
		{
			if (item is not RoomDto room)
			{
				return false;
			}

			string keyword = txtSearch.Text.Trim();
			bool matchesKeyword = string.IsNullOrEmpty(keyword) ||
				room.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				room.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				room.RoomTypeName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			bool matchesBuilding = cbBuilding.SelectedItem is not string building ||
				building == All || room.BuildingName == building;
			bool matchesStatus = cbStatus.SelectedItem is not string status ||
				status == All || room.StatusDisplay == status;
			bool matchesFloor = cbFloor.SelectedItem is not string floor ||
				floor == All || room.Floor.ToString() == floor;

			return matchesKeyword && matchesBuilding && matchesStatus && matchesFloor;
		}

		// Áp lại bộ lọc khi tìm kiếm / combo đổi
		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		// Làm mới view và đếm số dòng hiển thị
		private void RefreshView()
		{
			_roomView?.Refresh();
			int count = _roomView?.Cast<object>().Count() ?? 0;
			lbResultCount.Text = $"{count} phòng";
			SetState(count == 0 ? "Không có phòng phù hợp." : string.Empty, count == 0);
		}

		// Hiện / ẩn thông báo trạng thái trống
		private void SetState(string message, bool isVisible)
		{
			lbState.Text = message;
			brdState.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
