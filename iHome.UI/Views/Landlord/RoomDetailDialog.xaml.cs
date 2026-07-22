using iHome.BLL.DTOs.Landlord;
using System.Globalization;
using System.Windows;

namespace iHome.UI.Views.Landlord
{
	// Read-only room detail — room info and current tenants (no DB writes)
	public partial class RoomDetailDialog : Window
	{
		public RoomDetailDialog(RoomDetailDto detail)
		{
			InitializeComponent();
			lbTitle.Text = $"Chi tiết phòng {detail.RoomNumber}";
			lbProperty.Text = detail.PropertyName;
			lbBuilding.Text = detail.BuildingName;
			lbRoomNumber.Text = detail.RoomNumber;
			lbRoomType.Text = detail.RoomTypeName;
			lbFloor.Text = detail.Floor.ToString();
			lbArea.Text = detail.Area.HasValue
				? $"{detail.Area.Value.ToString("N1", CultureInfo.CurrentCulture)} m²"
				: "-";
			lbBaseRent.Text = $"{detail.BaseRent.ToString("N0", CultureInfo.CurrentCulture)} đ";
			lbStatus.Text = detail.StatusDisplay;
			lbOccupancy.Text = $"{detail.Tenants.Count}/{detail.MaxOccupancy}";
			lbNotes.Text = string.IsNullOrWhiteSpace(detail.Notes) ? "-" : detail.Notes;

			dgTenants.ItemsSource = detail.Tenants;
			bool empty = detail.Tenants.Count == 0;
			brdNoTenants.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
		}

		private void btnClose_Click(object sender, RoutedEventArgs e) => DialogResult = true;
	}
}
