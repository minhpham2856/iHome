using iHome.BLL.DTOs;
using iHome.BLL.Services.Manager;
using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace iHome.UI.Views.Manager
{
	public partial class RoomsPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _propertyId;
		private readonly ManagerRoomService _service = new();
		private List<ManagerRoomDto> _rooms = new();
		private ICollectionView? _roomView;
		private bool _isLoading;

		public RoomsPage(User currentUser, int? propertyId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_propertyId = propertyId;
			Loaded += RoomsPage_Loaded;
		}

		private async void RoomsPage_Loaded(object sender, RoutedEventArgs e) =>
			await LoadRoomsAsync();

		private async void BtnRefresh_Click(object sender, RoutedEventArgs e) =>
			await LoadRoomsAsync();

		private async Task LoadRoomsAsync()
		{
			if (_isLoading)
			{
				return;
			}

			try
			{
				_isLoading = true;
				BtnRefresh.IsEnabled = false;
				SetState("Đang tải danh sách phòng...", true);
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				_rooms = await Task.Run(() => _service.GetRooms(managerId, _propertyId));

				_roomView = CollectionViewSource.GetDefaultView(_rooms);
				_roomView.Filter = FilterRoom;
				RoomsGrid.ItemsSource = _roomView;
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
				BtnRefresh.IsEnabled = true;
			}
		}

		private void PopulateFilters()
		{
			CboBuilding.ItemsSource = new[] { All }
				.Concat(_rooms.Select(r => r.BuildingName).Distinct().OrderBy(name => name));
			CboStatus.ItemsSource = new[] { All }
				.Concat(_rooms.Select(r => r.StatusDisplay).Distinct().OrderBy(status => status));
			CboFloor.ItemsSource = new[] { All }
				.Concat(_rooms.Select(r => r.Floor.ToString()).Distinct().OrderBy(floor => floor));
			CboBuilding.SelectedIndex = 0;
			CboStatus.SelectedIndex = 0;
			CboFloor.SelectedIndex = 0;
		}

		private void UpdateSummary()
		{
			TxtTotalRooms.Text = _rooms.Count.ToString();
			TxtOccupiedRooms.Text = _rooms.Count(room =>
				string.Equals(room.Status, "Occupied", StringComparison.OrdinalIgnoreCase)).ToString();
			TxtVacantRooms.Text = _rooms.Count(room =>
				string.Equals(room.Status, "Vacant", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(room.Status, "Empty", StringComparison.OrdinalIgnoreCase)).ToString();
			TxtMaintenanceRooms.Text = _rooms.Count(room =>
				string.Equals(room.Status, "Maintenance", StringComparison.OrdinalIgnoreCase)).ToString();
		}

		private bool FilterRoom(object item)
		{
			if (item is not ManagerRoomDto room)
			{
				return false;
			}

			string keyword = TxtSearch.Text.Trim();
			bool matchesKeyword = string.IsNullOrEmpty(keyword) ||
				room.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				room.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
				room.RoomTypeName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			bool matchesBuilding = CboBuilding.SelectedItem is not string building ||
				building == All || room.BuildingName == building;
			bool matchesStatus = CboStatus.SelectedItem is not string status ||
				status == All || room.StatusDisplay == status;
			bool matchesFloor = CboFloor.SelectedItem is not string floor ||
				floor == All || room.Floor.ToString() == floor;

			return matchesKeyword && matchesBuilding && matchesStatus && matchesFloor;
		}

		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		private void RefreshView()
		{
			_roomView?.Refresh();
			int count = _roomView?.Cast<object>().Count() ?? 0;
			TxtResultCount.Text = $"{count} phòng";
			SetState(count == 0 ? "Không có phòng phù hợp." : string.Empty, count == 0);
		}

		private void SetState(string message, bool isVisible)
		{
			StateText.Text = message;
			StatePanel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
