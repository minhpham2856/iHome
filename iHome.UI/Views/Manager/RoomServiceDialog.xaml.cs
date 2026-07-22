using iHome.BLL.DTOs.Manager;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	// Dialog chọn phòng và dịch vụ để gán
	public partial class RoomServiceDialog : Window
	{
		private readonly List<LookupOptionDto> _services;
		public int RoomId { get; private set; }
		public int ServiceId { get; private set; }

		// Nạp danh sách phòng; dịch vụ lọc theo tòa của phòng
		public RoomServiceDialog(
			IEnumerable<LookupOptionDto> rooms,
			IEnumerable<LookupOptionDto> services)
		{
			InitializeComponent();
			_services = services.ToList();
			cbRoom.ItemsSource = rooms.ToList();
			cbRoom.SelectedIndex = cbRoom.Items.Count > 0 ? 0 : -1;
		}

		// Đổi phòng → chỉ hiện dịch vụ cùng tòa nhà
		private void RoomChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cbRoom.SelectedItem is not LookupOptionDto room)
			{
				cbService.ItemsSource = null;
				return;
			}
			cbService.ItemsSource = _services.Where(service => service.PropertyId == room.PropertyId).ToList();
			cbService.SelectedIndex = cbService.Items.Count > 0 ? 0 : -1;
		}

		// Lưu RoomId / ServiceId đã chọn
		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (cbRoom.SelectedItem is not LookupOptionDto room ||
				cbService.SelectedItem is not LookupOptionDto service)
			{
				ManagerUi.ShowValidation("Không có phòng hoặc dịch vụ phù hợp.");
				return;
			}
			RoomId = room.Id;
			ServiceId = service.Id;
			DialogResult = true;
		}

		// Đóng dialog không lưu
		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter → ô dịch vụ
		private void cbRoom_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cbService.Focus(); }
		}

		// Enter trên dịch vụ → lưu
		private void cbService_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				Save_Click(btnSave, new RoutedEventArgs());
			}
		}
	}
}
