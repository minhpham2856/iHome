using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Enums;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// dialog thêm/sửa phòng trong một tòa — chọn loại phòng, tầng, trạng thái; khóa status nếu phòng đang có người ở
	public partial class RoomDialog : Window
	{
		private readonly int _roomId;
		private readonly int _buildingId;
		public RoomFormDto? Result { get; private set; }

		public RoomDialog(
			int buildingId,
			List<RoomTypeOptionDto> roomTypes,
			RoomFormDto? room = null,
			bool hasOccupants = false)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside RoomDialog without altering business rules
			_buildingId = buildingId;
			// Assign local/page state inside RoomDialog without altering business rules
			_roomId = room?.Id ?? 0;

			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbRoomType.ItemsSource = roomTypes;
			// Assign local/page state inside RoomDialog without altering business rules
			var statusOptions = new[]
			{
				// Execute UI step inside RoomDialog
				new StatusOption(RoomStatus.Empty, RoomStatus.Empty),
				// Execute UI step inside RoomDialog
				new StatusOption(RoomStatus.Occupied, RoomStatus.Occupied),
				// Execute UI step inside RoomDialog
				new StatusOption(RoomStatus.Deposited, RoomStatus.Deposited),
				// Execute UI step inside RoomDialog
				new StatusOption(RoomStatus.Maintenance, RoomStatus.Maintenance)
			};
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbStatus.ItemsSource = statusOptions;

			// Guard clause: only continue when UI selection, role, or input is valid
			if (room == null)
			{
				// Update TextBlock/TextBox caption or read user-entered text from control
				txtFloor.Text = "1";
				// Pick default combo index (usually first/all option) after reload
				cbRoomType.SelectedIndex = roomTypes.Count > 0 ? 0 : -1;
				// Pick default combo index (usually first/all option) after reload
				cbStatus.SelectedIndex = 0;
				// Execute UI step inside RoomDialog
				UpdateTypeInfo();
				// Exit method early or return value/tuple to caller
				return;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTitle.Text = "Cập nhật phòng";
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtRoomNumber.Text = room.RoomNumber;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtFloor.Text = room.Floor.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtNotes.Text = room.Notes ?? string.Empty;
			// Assign local/page state inside RoomDialog without altering business rules
			cbRoomType.SelectedValue = room.RoomTypeId;
			// Restore or set combo selection to match entity id or filter
			if (cbRoomType.SelectedItem == null && roomTypes.Count > 0)
			{
				// Pick default combo index (usually first/all option) after reload
				cbRoomType.SelectedIndex = 0;
			}
			// Execute UI step inside RoomDialog
			UpdateTypeInfo();

			// phòng có khách — không cho đổi trạng thái khỏi Occupied (BLL cũng enforce)
			if (hasOccupants)
			{
				// Restore or set combo selection to match entity id or filter
				cbStatus.SelectedItem = statusOptions.First(s => s.Code == RoomStatus.Occupied);
				// Enable/disable control during loading or when prerequisites missing
				cbStatus.IsEnabled = false;
			}
			// Alternate branch when previous condition was not satisfied
			else
			{
				// Assign local/page state inside RoomDialog without altering business rules
				string code = room.Status switch
				{
					// Assign local/page state inside RoomDialog without altering business rules
					RoomStatus.Occupied => RoomStatus.Occupied,
					// Assign local/page state inside RoomDialog without altering business rules
					RoomStatus.Deposited => RoomStatus.Deposited,
					// Assign local/page state inside RoomDialog without altering business rules
					RoomStatus.Maintenance => RoomStatus.Maintenance,
					// Assign local/page state inside RoomDialog without altering business rules
					_ => RoomStatus.Empty
				};
				// Restore or set combo selection to match entity id or filter
				cbStatus.SelectedItem = statusOptions.FirstOrDefault(s => s.Code == code) ?? statusOptions[0];
			}
		}

		// hiển thị diện tích và giá thuê cơ bản của loại phòng đang chọn (read-only hint)
		private void UpdateTypeInfo()
		{
			// Change combo selection to drive filter cascade or dialog default
			if (cbRoomType.SelectedItem is RoomTypeOptionDto type)
			{
				// Assign local/page state inside UpdateTypeInfo without altering business rules
				string area = type.Area.HasValue
					// Execute UI step inside UpdateTypeInfo
					? $"{type.Area.Value.ToString("N1", CultureInfo.CurrentCulture)} m²"
					// Execute UI step inside UpdateTypeInfo
					: "-";
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbTypeInfo.Text = $"{area} · {type.BaseRent.ToString("N0", CultureInfo.CurrentCulture)} đ";
			}
			// Alternate branch when previous condition was not satisfied
			else
			{
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbTypeInfo.Text = "-";
			}
		}

		// Assign local/page state inside cbRoomType_SelectionChanged without altering business rules
		private void cbRoomType_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateTypeInfo();

		// validate số phòng, loại phòng, tầng ≥ 1
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (string.IsNullOrWhiteSpace(txtRoomNumber.Text))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Số phòng không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Change combo selection to drive filter cascade or dialog default
			if (cbRoomType.SelectedItem is not RoomTypeOptionDto)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn loại phòng.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (!int.TryParse(txtFloor.Text.Trim(), out int floor) || floor < 1)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Tầng phải lớn hơn hoặc bằng 1.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Change combo selection to drive filter cascade or dialog default
			var status = cbStatus.SelectedItem as StatusOption;
			// Work with BLL DTO/form object returned from service or built from controls
			Result = new RoomFormDto
			{
				// Assign local/page state inside btnSave_Click without altering business rules
				Id = _roomId,
				// Assign local/page state inside btnSave_Click without altering business rules
				BuildingId = _buildingId,
				// Change combo selection to drive filter cascade or dialog default
				RoomTypeId = ((RoomTypeOptionDto)cbRoomType.SelectedItem).Id,
				// Update TextBlock/TextBox caption or read user-entered text from control
				RoomNumber = txtRoomNumber.Text,
				// Assign local/page state inside btnSave_Click without altering business rules
				Floor = floor,
				// Assign local/page state inside btnSave_Click without altering business rules
				Status = status?.Code ?? RoomStatus.Empty,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Notes = txtNotes.Text
			};
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// cặp Code/Name cho ComboBox trạng thái phòng
		private sealed record StatusOption(string Code, string Name);

		// Enter — tab order qua các control nhập liệu
		private void txtRoomNumber_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; cbRoomType.Focus(); }
		}

		private void cbRoomType_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtFloor.Focus(); }
		}

		private void txtFloor_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; cbStatus.Focus(); }
		}

		private void cbStatus_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; btnSave.Focus(); }
		}
	}
}

