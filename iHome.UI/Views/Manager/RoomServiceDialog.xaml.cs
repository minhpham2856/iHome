using iHome.BLL.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
			CboRoom.ItemsSource = rooms.ToList();
			CboRoom.SelectedIndex = CboRoom.Items.Count > 0 ? 0 : -1;
		}

		private void RoomChanged(object sender, SelectionChangedEventArgs e)
		{
			if (CboRoom.SelectedItem is not ManagerLookupOptionDto room)
			{
				CboService.ItemsSource = null;
				return;
			}
			CboService.ItemsSource = _services.Where(service => service.BuildingId == room.BuildingId).ToList();
			CboService.SelectedIndex = CboService.Items.Count > 0 ? 0 : -1;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (CboRoom.SelectedItem is not ManagerLookupOptionDto room ||
				CboService.SelectedItem is not ManagerLookupOptionDto service)
			{
				ManagerUi.ShowValidation("Không có phòng hoặc dịch vụ phù hợp.");
				return;
			}
			RoomId = room.Id;
			ServiceId = service.Id;
			DialogResult = true;
		}

		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
	}
}
