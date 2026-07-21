using iHome.BLL.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	public partial class RoomServiceDialog : Window
	{
		private readonly List<ManagerLookupOptionDto> _services;
		public int RoomId { get; private set; }
		public int ServiceId { get; private set; }

		public RoomServiceDialog(
			IEnumerable<ManagerLookupOptionDto> rooms,
			IEnumerable<ManagerLookupOptionDto> services)
		{
			InitializeComponent();
			_services = services.ToList();
			CbRoom.ItemsSource = rooms.ToList();
			CbRoom.SelectedIndex = CbRoom.Items.Count > 0 ? 0 : -1;
		}

		private void RoomChanged(object sender, SelectionChangedEventArgs e)
		{
			if (CbRoom.SelectedItem is not ManagerLookupOptionDto room)
			{
				CbService.ItemsSource = null;
				return;
			}
			CbService.ItemsSource = _services.Where(service => service.PropertyId == room.PropertyId).ToList();
			CbService.SelectedIndex = CbService.Items.Count > 0 ? 0 : -1;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (CbRoom.SelectedItem is not ManagerLookupOptionDto room ||
				CbService.SelectedItem is not ManagerLookupOptionDto service)
			{
				ManagerUi.ShowValidation("Không có phòng hoặc dịch vụ phù hợp.");
				return;
			}
			RoomId = room.Id;
			ServiceId = service.Id;
			DialogResult = true;
		}

		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// key down enter
		private void CbRoom_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; CbService.Focus(); }
		}

		private void CbService_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				Save_Click(BtnSave, new RoutedEventArgs());
			}
		}
	}
}
