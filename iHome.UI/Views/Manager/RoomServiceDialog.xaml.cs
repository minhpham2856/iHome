using iHome.BLL.DTOs.Manager;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	// Code-behind for the RoomServiceDialog modal dialog.
	public partial class RoomServiceDialog : Window
	{
		private readonly List<LookupOptionDto> _services;
		public int RoomId { get; private set; }
		public int ServiceId { get; private set; }

		public RoomServiceDialog(
			IEnumerable<LookupOptionDto> rooms,
			IEnumerable<LookupOptionDto> services)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Materialize query to List for repeated binding and filtering
			_services = services.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbRoom.ItemsSource = rooms.ToList();
			// Pick default combo index (usually first/all option) after reload
			cbRoom.SelectedIndex = cbRoom.Items.Count > 0 ? 0 : -1;
		}

		private void RoomChanged(object sender, SelectionChangedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (cbRoom.SelectedItem is not LookupOptionDto room)
			{
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				cbService.ItemsSource = null;
				// Exit method early or return value/tuple to caller
				return;
			}
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbService.ItemsSource = _services.Where(service => service.PropertyId == room.PropertyId).ToList();
			// Pick default combo index (usually first/all option) after reload
			cbService.SelectedIndex = cbService.Items.Count > 0 ? 0 : -1;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (cbRoom.SelectedItem is not LookupOptionDto room ||
				// Change combo selection to drive filter cascade or dialog default
				cbService.SelectedItem is not LookupOptionDto service)
			{
				// ManagerUi.ShowValidation shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowValidation("Không có phòng hoặc dịch vụ phù hợp.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Assign local/page state inside Save_Click without altering business rules
			RoomId = room.Id;
			// Assign local/page state inside Save_Click without altering business rules
			ServiceId = service.Id;
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private void cbRoom_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; cbService.Focus(); }
		}

		private void cbService_KeyDown(object sender, KeyEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (e.Key == Key.Enter)
			{
				// Consume Enter key so WPF does not trigger default button twice
				e.Handled = true;
				// Handle Enter key to move focus or submit like clicking the primary button
				Save_Click(btnSave, new RoutedEventArgs());
			}
		}
	}
}
