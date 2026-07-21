using iHome.BLL.DTOs.Landlord;
using System.Globalization;
using System.Windows;

namespace iHome.UI.Views.Landlord
{
	public partial class RoomDetailDialog : Window
	{
		public RoomDetailDialog(RoomDetailDto detail)
		{
			InitializeComponent();
			lblTitle.Text = $"Chi tiết phòng {detail.RoomNumber}";
			lblProperty.Text = detail.PropertyName;
			lblBuilding.Text = detail.BuildingName;
			lblRoomNumber.Text = detail.RoomNumber;
			lblRoomType.Text = detail.RoomTypeName;
			lblFloor.Text = detail.Floor.ToString();
			lblArea.Text = detail.Area.HasValue
				? $"{detail.Area.Value.ToString("N1", CultureInfo.CurrentCulture)} m²"
				: "-";
			lblBaseRent.Text = $"{detail.BaseRent.ToString("N0", CultureInfo.CurrentCulture)} đ";
			lblStatus.Text = detail.StatusDisplay;
			lblOccupancy.Text = $"{detail.Tenants.Count}/{detail.MaxOccupancy}";
			lblNotes.Text = string.IsNullOrWhiteSpace(detail.Notes) ? "-" : detail.Notes;

			dgTenants.ItemsSource = detail.Tenants;
			bool empty = detail.Tenants.Count == 0;
			brdNoTenants.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
		}

		private void btnClose_Click(object sender, RoutedEventArgs e) => DialogResult = true;
	}
}
