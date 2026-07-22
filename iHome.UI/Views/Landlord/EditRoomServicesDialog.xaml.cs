using iHome.BLL.DTOs.Landlord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// Assign services to one room — pick room, tick services; loadServices lazy by roomId
	public partial class EditRoomServicesDialog : Window
	{
		// Factory loads service checkboxes for a room (keeps BLL out of this dialog)
		private readonly Func<int, List<AssignCheckOptionDto>> _loadServices;
		private List<AssignCheckOptionDto> _services = new();

		public int? RoomId { get; private set; }
		public List<int> SelectedServiceIds { get; private set; } = new();

		public EditRoomServicesDialog(
			IEnumerable<RoomServiceSummaryDto> rooms,
			int? preferredRoomId,
			Func<int, List<AssignCheckOptionDto>> loadServices)
		{
			InitializeComponent();
			_loadServices = loadServices ?? throw new ArgumentNullException(nameof(loadServices));

			var list = rooms.ToList();
			cbRoom.ItemsSource = list;
			if (preferredRoomId.HasValue)
			{
				cbRoom.SelectedItem = list.FirstOrDefault(r => r.RoomId == preferredRoomId.Value);
			}
			if (cbRoom.SelectedIndex < 0 && list.Count > 0)
			{
				cbRoom.SelectedIndex = 0;
			}
		}

		// Room change reloads services with current tick state from BLL
		private void cbRoom_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (cbRoom.SelectedItem is not RoomServiceSummaryDto room)
			{
				lstServices.ItemsSource = null;
				_services = new();
				return;
			}

			try
			{
				_services = _loadServices(room.RoomId);
				lstServices.ItemsSource = _services;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				_services = new();
				lstServices.ItemsSource = null;
			}
		}

		private void btnSelectAll_Click(object sender, RoutedEventArgs e)
		{
			foreach (var service in _services) service.IsSelected = true;
			lstServices.Items.Refresh();
		}

		private void btnClear_Click(object sender, RoutedEventArgs e)
		{
			foreach (var service in _services) service.IsSelected = false;
			lstServices.Items.Refresh();
		}

		// Return RoomId + SelectedServiceIds for ServicesPage.SyncRoomAssignments
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (cbRoom.SelectedItem is not RoomServiceSummaryDto room)
			{
				MessageBox.Show("Vui lòng chọn phòng.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			RoomId = room.RoomId;
			SelectedServiceIds = _services.Where(s => s.IsSelected).Select(s => s.Id).ToList();
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private void cbRoom_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; lstServices.Focus(); }
		}
	}
}
