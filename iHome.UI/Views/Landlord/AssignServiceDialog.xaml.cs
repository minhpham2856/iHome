using iHome.BLL.DTOs.Landlord;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace iHome.UI.Views.Landlord
{
	// dialog gán hàng loạt dịch vụ cho nhiều phòng — checkbox list phòng + dịch vụ; kết quả trả về id đã chọn
	public partial class AssignServiceDialog : Window
	{
		// danh sách checkbox phòng và dịch vụ — mutate IsSelected tại chỗ rồi Refresh ItemsControl
		private readonly List<AssignCheckOptionDto> _rooms;
		private readonly List<AssignCheckOptionDto> _services;

		// id phòng và dịch vụ người dùng chọn khi bấm Lưu — ServicesPage đọc sau ShowDialog
		public List<int> SelectedRoomIds { get; private set; } = new();
		public List<int> SelectedServiceIds { get; private set; } = new();

		public AssignServiceDialog(
			IEnumerable<AssignCheckOptionDto> rooms,
			IEnumerable<AssignCheckOptionDto> services)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Materialize query to List for repeated binding and filtering
			_rooms = rooms.ToList();
			// Materialize query to List for repeated binding and filtering
			_services = services.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			lstRooms.ItemsSource = _rooms;
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			lstServices.ItemsSource = _services;
		}

		// chọn tất cả phòng trong danh sách
		private void btnSelectAllRooms_Click(object sender, RoutedEventArgs e)
		{
			// Iterate collection to update UI, chart series, or CSV rows
			foreach (var room in _rooms) room.IsSelected = true;
			// Reapply ICollectionView filter after search or combo change
			lstRooms.Items.Refresh();
		}

		// bỏ chọn tất cả phòng
		private void btnClearRooms_Click(object sender, RoutedEventArgs e)
		{
			// Iterate collection to update UI, chart series, or CSV rows
			foreach (var room in _rooms) room.IsSelected = false;
			// Reapply ICollectionView filter after search or combo change
			lstRooms.Items.Refresh();
		}

		// chọn tất cả dịch vụ
		private void btnSelectAllServices_Click(object sender, RoutedEventArgs e)
		{
			// Iterate collection to update UI, chart series, or CSV rows
			foreach (var service in _services) service.IsSelected = true;
			// Reapply ICollectionView filter after search or combo change
			lstServices.Items.Refresh();
		}

		// bỏ chọn tất cả dịch vụ
		private void btnClearServices_Click(object sender, RoutedEventArgs e)
		{
			// Iterate collection to update UI, chart series, or CSV rows
			foreach (var service in _services) service.IsSelected = false;
			// Reapply ICollectionView filter after search or combo change
			lstServices.Items.Refresh();
		}

		// validate ít nhất một phòng và một dịch vụ rồi đóng dialog thành công
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			// LINQ step to shape in-memory list for filters, KPIs, or binding
			SelectedRoomIds = _rooms.Where(r => r.IsSelected).Select(r => r.Id).ToList();
			// LINQ step to shape in-memory list for filters, KPIs, or binding
			SelectedServiceIds = _services.Where(s => s.IsSelected).Select(s => s.Id).ToList();

			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedRoomIds.Count == 0)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn ít nhất một phòng.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedServiceIds.Count == 0)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn ít nhất một dịch vụ.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
	}
}
