using iHome.BLL.DTOs.Landlord;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace iHome.UI.Views.Landlord
{
	public partial class AssignServiceDialog : Window
	{
		private readonly List<AssignCheckOptionDto> _rooms;
		private readonly List<AssignCheckOptionDto> _services;

		public List<int> SelectedRoomIds { get; private set; } = new();
		public List<int> SelectedServiceIds { get; private set; } = new();

		public AssignServiceDialog(
			IEnumerable<AssignCheckOptionDto> rooms,
			IEnumerable<AssignCheckOptionDto> services)
		{
			InitializeComponent();
			_rooms = rooms.ToList();
			_services = services.ToList();
			lstRooms.ItemsSource = _rooms;
			lstServices.ItemsSource = _services;
		}

		private void btnSelectAllRooms_Click(object sender, RoutedEventArgs e)
		{
			foreach (var room in _rooms) room.IsSelected = true;
			lstRooms.Items.Refresh();
		}

		private void btnClearRooms_Click(object sender, RoutedEventArgs e)
		{
			foreach (var room in _rooms) room.IsSelected = false;
			lstRooms.Items.Refresh();
		}

		private void btnSelectAllServices_Click(object sender, RoutedEventArgs e)
		{
			foreach (var service in _services) service.IsSelected = true;
			lstServices.Items.Refresh();
		}

		private void btnClearServices_Click(object sender, RoutedEventArgs e)
		{
			foreach (var service in _services) service.IsSelected = false;
			lstServices.Items.Refresh();
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			SelectedRoomIds = _rooms.Where(r => r.IsSelected).Select(r => r.Id).ToList();
			SelectedServiceIds = _services.Where(s => s.IsSelected).Select(s => s.Id).ToList();

			if (SelectedRoomIds.Count == 0)
			{
				MessageBox.Show("Vui lòng chọn ít nhất một phòng.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (SelectedServiceIds.Count == 0)
			{
				MessageBox.Show("Vui lòng chọn ít nhất một dịch vụ.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
	}
}
