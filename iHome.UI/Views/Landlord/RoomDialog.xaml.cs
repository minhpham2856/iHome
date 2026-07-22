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
	// Add/edit room in a building — type, floor, status; status locked when occupied
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
			InitializeComponent();
			_buildingId = buildingId;
			_roomId = room?.Id ?? 0;

			cbRoomType.ItemsSource = roomTypes;
			var statusOptions = new[]
			{
				new StatusOption(RoomStatus.Empty, RoomStatus.Empty),
				new StatusOption(RoomStatus.Occupied, RoomStatus.Occupied),
				new StatusOption(RoomStatus.Deposited, RoomStatus.Deposited),
				new StatusOption(RoomStatus.Maintenance, RoomStatus.Maintenance)
			};
			cbStatus.ItemsSource = statusOptions;

			if (room == null)
			{
				txtFloor.Text = "1";
				cbRoomType.SelectedIndex = roomTypes.Count > 0 ? 0 : -1;
				cbStatus.SelectedIndex = 0;
				UpdateTypeInfo();
				return;
			}

			lbTitle.Text = "Cập nhật phòng";
			txtRoomNumber.Text = room.RoomNumber;
			txtFloor.Text = room.Floor.ToString();
			txtNotes.Text = room.Notes ?? string.Empty;
			cbRoomType.SelectedValue = room.RoomTypeId;
			if (cbRoomType.SelectedItem == null && roomTypes.Count > 0)
			{
				cbRoomType.SelectedIndex = 0;
			}
			UpdateTypeInfo();

			// Occupied rooms cannot leave Occupied status (BLL also enforces)
			if (hasOccupants)
			{
				cbStatus.SelectedItem = statusOptions.First(s => s.Code == RoomStatus.Occupied);
				cbStatus.IsEnabled = false;
			}
			else
			{
				string code = room.Status switch
				{
					RoomStatus.Occupied => RoomStatus.Occupied,
					RoomStatus.Deposited => RoomStatus.Deposited,
					RoomStatus.Maintenance => RoomStatus.Maintenance,
					_ => RoomStatus.Empty
				};
				cbStatus.SelectedItem = statusOptions.FirstOrDefault(s => s.Code == code) ?? statusOptions[0];
			}
		}

		// Show selected room-type area and base rent (read-only hint)
		private void UpdateTypeInfo()
		{
			if (cbRoomType.SelectedItem is RoomTypeOptionDto type)
			{
				string area = type.Area.HasValue
					? $"{type.Area.Value.ToString("N1", CultureInfo.CurrentCulture)} m²"
					: "-";
				lbTypeInfo.Text = $"{area} · {type.BaseRent.ToString("N0", CultureInfo.CurrentCulture)} đ";
			}
			else
			{
				lbTypeInfo.Text = "-";
			}
		}

		private void cbRoomType_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateTypeInfo();

		// Validate room number, type, and floor ≥ 1
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtRoomNumber.Text))
			{
				MessageBox.Show("Số phòng không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (cbRoomType.SelectedItem is not RoomTypeOptionDto)
			{
				MessageBox.Show("Vui lòng chọn loại phòng.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (!int.TryParse(txtFloor.Text.Trim(), out int floor) || floor < 1)
			{
				MessageBox.Show("Tầng phải lớn hơn hoặc bằng 1.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var status = cbStatus.SelectedItem as StatusOption;
			Result = new RoomFormDto
			{
				Id = _roomId,
				BuildingId = _buildingId,
				RoomTypeId = ((RoomTypeOptionDto)cbRoomType.SelectedItem).Id,
				RoomNumber = txtRoomNumber.Text,
				Floor = floor,
				Status = status?.Code ?? RoomStatus.Empty,
				Notes = txtNotes.Text
			};
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Code/Name pair for room-status ComboBox
		private sealed record StatusOption(string Code, string Name);

		// Enter moves focus through input controls
		private void txtRoomNumber_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cbRoomType.Focus(); }
		}

		private void cbRoomType_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtFloor.Focus(); }
		}

		private void txtFloor_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cbStatus.Focus(); }
		}

		private void cbStatus_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; btnSave.Focus(); }
		}
	}
}
