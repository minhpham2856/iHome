using iHome.BLL.DTOs.Landlord;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
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

			cboRoomType.ItemsSource = roomTypes;
			var statusOptions = new[]
			{
				new StatusOption("Empty", "Còn trống"),
				new StatusOption("Occupied", "Đang ở"),
				new StatusOption("Deposited", "Đã đặt cọc")
			};
			cboStatus.ItemsSource = statusOptions;

			if (room == null)
			{
				txtFloor.Text = "1";
				cboRoomType.SelectedIndex = roomTypes.Count > 0 ? 0 : -1;
				cboStatus.SelectedIndex = 0;
				UpdateTypeInfo();
				return;
			}

			lblTitle.Text = "Cập nhật phòng";
			txtRoomNumber.Text = room.RoomNumber;
			txtFloor.Text = room.Floor.ToString();
			txtNotes.Text = room.Notes ?? string.Empty;
			cboRoomType.SelectedValue = room.RoomTypeId;
			if (cboRoomType.SelectedItem == null && roomTypes.Count > 0)
			{
				cboRoomType.SelectedIndex = 0;
			}
			UpdateTypeInfo();

			if (hasOccupants)
			{
				cboStatus.SelectedItem = statusOptions.First(s => s.Code == "Occupied");
				cboStatus.IsEnabled = false;
			}
			else
			{
				string code = room.Status switch
				{
					"Occupied" => "Occupied",
					"Deposited" => "Deposited",
					_ => "Empty"
				};
				cboStatus.SelectedItem = statusOptions.FirstOrDefault(s => s.Code == code) ?? statusOptions[0];
			}
		}

		private void UpdateTypeInfo()
		{
			if (cboRoomType.SelectedItem is RoomTypeOptionDto type)
			{
				string area = type.Area.HasValue
					? $"{type.Area.Value.ToString("N1", CultureInfo.CurrentCulture)} m²"
					: "-";
				lblTypeInfo.Text = $"{area} · {type.BaseRent.ToString("N0", CultureInfo.CurrentCulture)} đ";
			}
			else
			{
				lblTypeInfo.Text = "-";
			}
		}

		private void cboRoomType_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateTypeInfo();

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtRoomNumber.Text))
			{
				MessageBox.Show("Số phòng không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (cboRoomType.SelectedItem is not RoomTypeOptionDto)
			{
				MessageBox.Show("Vui lòng chọn loại phòng.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (!int.TryParse(txtFloor.Text.Trim(), out int floor) || floor < 1)
			{
				MessageBox.Show("Tầng phải lớn hơn hoặc bằng 1.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var status = cboStatus.SelectedItem as StatusOption;
			Result = new RoomFormDto
			{
				Id = _roomId,
				BuildingId = _buildingId,
				RoomTypeId = ((RoomTypeOptionDto)cboRoomType.SelectedItem).Id,
				RoomNumber = txtRoomNumber.Text,
				Floor = floor,
				Status = status?.Code ?? "Empty",
				Notes = txtNotes.Text
			};
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private sealed record StatusOption(string Code, string Name);

		// key down enter
		private void txtRoomNumber_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cboRoomType.Focus(); }
		}

		private void cboRoomType_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtFloor.Focus(); }
		}

		private void txtFloor_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cboStatus.Focus(); }
		}

		private void cboStatus_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; btnSave.Focus(); }
		}
	}
}
