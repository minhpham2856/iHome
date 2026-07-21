using iHome.BLL.DTOs.Landlord;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	public partial class RoomTypeDialog : Window
	{
		private readonly int _roomTypeId;
		private readonly int _propertyId;
		public RoomTypeFormDto? Result { get; private set; }

		public RoomTypeDialog(int propertyId, RoomTypeFormDto? roomType = null)
		{
			InitializeComponent();
			_propertyId = propertyId;
			_roomTypeId = roomType?.Id ?? 0;

			if (roomType == null)
			{
				txtMaxOccupancy.Text = "1";
				txtBaseRent.Text = "0";
				return;
			}

			lblTitle.Text = "Cập nhật loại phòng";
			txtTypeName.Text = roomType.TypeName;
			txtMaxOccupancy.Text = roomType.MaxOccupancy.ToString();
			txtArea.Text = roomType.Area?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
			txtBaseRent.Text = roomType.BaseRent.ToString(CultureInfo.CurrentCulture);
			txtDescription.Text = roomType.Description ?? string.Empty;
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtTypeName.Text))
			{
				MessageBox.Show("Tên loại phòng không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (!int.TryParse(txtMaxOccupancy.Text.Trim(), out int max) || max < 1)
			{
				MessageBox.Show("Số người tối đa phải lớn hơn hoặc bằng 1.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (!decimal.TryParse(txtBaseRent.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal rent) || rent < 0)
			{
				MessageBox.Show("Giá thuê không hợp lệ.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			decimal? area = null;
			if (!string.IsNullOrWhiteSpace(txtArea.Text))
			{
				if (!decimal.TryParse(txtArea.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal parsedArea) || parsedArea < 0)
				{
					MessageBox.Show("Diện tích không hợp lệ.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
				area = parsedArea;
			}

			Result = new RoomTypeFormDto
			{
				Id = _roomTypeId,
				PropertyId = _propertyId,
				TypeName = txtTypeName.Text,
				MaxOccupancy = max,
				Area = area,
				BaseRent = rent,
				Description = txtDescription.Text
			};
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// key down enter
		private void txtTypeName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtMaxOccupancy.Focus(); }
		}

		private void txtMaxOccupancy_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtArea.Focus(); }
		}

		private void txtArea_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtBaseRent.Focus(); }
		}

		private void txtBaseRent_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; btnSave.Focus(); }
		}
	}
}
