using iHome.BLL.DTOs.Landlord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	public partial class EditRoomServicesDialog : Window
	{
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
			cboRoom.ItemsSource = list;
			if (preferredRoomId.HasValue)
			{
				cboRoom.SelectedItem = list.FirstOrDefault(r => r.RoomId == preferredRoomId.Value);
			}
			if (cboRoom.SelectedIndex < 0 && list.Count > 0)
			{
				cboRoom.SelectedIndex = 0;
			}
		}

		private void cboRoom_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (cboRoom.SelectedItem is not RoomServiceSummaryDto room)
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

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (cboRoom.SelectedItem is not RoomServiceSummaryDto room)
			{
				MessageBox.Show("Vui lòng chọn phòng.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			RoomId = room.RoomId;
			SelectedServiceIds = _services.Where(s => s.IsSelected).Select(s => s.Id).ToList();
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// key down enter
		private void cboRoom_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; lstServices.Focus(); }
		}
	}
}
