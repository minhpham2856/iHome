using iHome.BLL.DTOs.Landlord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// dialog gán dịch vụ cho một phòng — chọn phòng rồi tick dịch vụ; loadServices lazy theo roomId
	public partial class EditRoomServicesDialog : Window
	{
		// factory load checkbox dịch vụ cho phòng — tránh dialog giữ reference tới LandlordServiceService
		private readonly Func<int, List<AssignCheckOptionDto>> _loadServices;
		private List<AssignCheckOptionDto> _services = new();

		public int? RoomId { get; private set; }
		public List<int> SelectedServiceIds { get; private set; } = new();

		public EditRoomServicesDialog(
			IEnumerable<RoomServiceSummaryDto> rooms,
			int? preferredRoomId,
			Func<int, List<AssignCheckOptionDto>> loadServices)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_loadServices = loadServices ?? throw new ArgumentNullException(nameof(loadServices));

			// Materialize query to List for repeated binding and filtering
			var list = rooms.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbRoom.ItemsSource = list;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (preferredRoomId.HasValue)
			{
				// Restore or set combo selection to match entity id or filter
				cbRoom.SelectedItem = list.FirstOrDefault(r => r.RoomId == preferredRoomId.Value);
			}
			// Change combo selection to drive filter cascade or dialog default
			if (cbRoom.SelectedIndex < 0 && list.Count > 0)
			{
				// Pick default combo index (usually first/all option) after reload
				cbRoom.SelectedIndex = 0;
			}
		}

		// đổi phòng — tải lại danh sách dịch vụ với trạng thái tick hiện tại từ BLL
		private void cbRoom_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (cbRoom.SelectedItem is not RoomServiceSummaryDto room)
			{
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				lstServices.ItemsSource = null;
				// Assign local/page state inside cbRoom_SelectionChanged without altering business rules
				_services = new();
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Assign local/page state inside cbRoom_SelectionChanged without altering business rules
				_services = _loadServices(room.RoomId);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				lstServices.ItemsSource = _services;
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Assign local/page state inside cbRoom_SelectionChanged without altering business rules
				_services = new();
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				lstServices.ItemsSource = null;
			}
		}

		private void btnSelectAll_Click(object sender, RoutedEventArgs e)
		{
			// Iterate collection to update UI, chart series, or CSV rows
			foreach (var service in _services) service.IsSelected = true;
			// Reapply ICollectionView filter after search or combo change
			lstServices.Items.Refresh();
		}

		private void btnClear_Click(object sender, RoutedEventArgs e)
		{
			// Iterate collection to update UI, chart series, or CSV rows
			foreach (var service in _services) service.IsSelected = false;
			// Reapply ICollectionView filter after search or combo change
			lstServices.Items.Refresh();
		}

		// trả RoomId + SelectedServiceIds cho ServicesPage.SyncRoomAssignments
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (cbRoom.SelectedItem is not RoomServiceSummaryDto room)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn phòng.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside btnSave_Click without altering business rules
			RoomId = room.RoomId;
			// LINQ step to shape in-memory list for filters, KPIs, or binding
			SelectedServiceIds = _services.Where(s => s.IsSelected).Select(s => s.Id).ToList();
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter ở combo phòng — chuyển focus sang list dịch vụ
		private void cbRoom_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; lstServices.Focus(); }
		}
	}
}
