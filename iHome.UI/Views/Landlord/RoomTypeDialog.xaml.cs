using iHome.BLL.DTOs.Landlord;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// dialog thêm/sửa loại phòng (RoomType) — thuộc một Property; định nghĩa giá/diện tích/sức chứa mặc định cho phòng
	public partial class RoomTypeDialog : Window
	{
		private readonly int _roomTypeId;
		private readonly int _propertyId;
		public RoomTypeFormDto? Result { get; private set; }

		public RoomTypeDialog(int propertyId, RoomTypeFormDto? roomType = null)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside RoomTypeDialog without altering business rules
			_propertyId = propertyId;
			// Assign local/page state inside RoomTypeDialog without altering business rules
			_roomTypeId = roomType?.Id ?? 0;

			// Guard clause: only continue when UI selection, role, or input is valid
			if (roomType == null)
			{
				// Update TextBlock/TextBox caption or read user-entered text from control
				txtMaxOccupancy.Text = "1";
				// Update TextBlock/TextBox caption or read user-entered text from control
				txtBaseRent.Text = "0";
				// Exit method early or return value/tuple to caller
				return;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTitle.Text = "Cập nhật loại phòng";
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtTypeName.Text = roomType.TypeName;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtMaxOccupancy.Text = roomType.MaxOccupancy.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtArea.Text = roomType.Area?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtBaseRent.Text = roomType.BaseRent.ToString(CultureInfo.CurrentCulture);
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtDescription.Text = roomType.Description ?? string.Empty;
		}

		// validate tên, sức chứa, giá; diện tích tùy chọn
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (string.IsNullOrWhiteSpace(txtTypeName.Text))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Tên loại phòng không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (!int.TryParse(txtMaxOccupancy.Text.Trim(), out int max) || max < 1)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Số người tối đa phải lớn hơn hoặc bằng 1.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (!decimal.TryParse(txtBaseRent.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal rent) || rent < 0)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Giá thuê không hợp lệ.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside btnSave_Click without altering business rules
			decimal? area = null;
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (!string.IsNullOrWhiteSpace(txtArea.Text))
			{
				// Update TextBlock/TextBox caption or read user-entered text from control
				if (!decimal.TryParse(txtArea.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal parsedArea) || parsedArea < 0)
				{
					// Show Vietnamese MessageBox to confirm, warn, or report success/failure
					MessageBox.Show("Diện tích không hợp lệ.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
					// Exit method early or return value/tuple to caller
					return;
				}
				// Assign local/page state inside btnSave_Click without altering business rules
				area = parsedArea;
			}

			// Work with BLL DTO/form object returned from service or built from controls
			Result = new RoomTypeFormDto
			{
				// Assign local/page state inside btnSave_Click without altering business rules
				Id = _roomTypeId,
				// Assign local/page state inside btnSave_Click without altering business rules
				PropertyId = _propertyId,
				// Update TextBlock/TextBox caption or read user-entered text from control
				TypeName = txtTypeName.Text,
				// Assign local/page state inside btnSave_Click without altering business rules
				MaxOccupancy = max,
				// Assign local/page state inside btnSave_Click without altering business rules
				Area = area,
				// Assign local/page state inside btnSave_Click without altering business rules
				BaseRent = rent,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Description = txtDescription.Text
			};
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter — điều hướng focus theo thứ tự nhập liệu
		private void txtTypeName_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtMaxOccupancy.Focus(); }
		}

		private void txtMaxOccupancy_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtArea.Focus(); }
		}

		private void txtArea_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtBaseRent.Focus(); }
		}

		private void txtBaseRent_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; btnSave.Focus(); }
		}
	}
}
