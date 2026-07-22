using iHome.BLL.DTOs.Landlord;
using System.Globalization;
using System.Windows;

namespace iHome.UI.Views.Landlord
{
	// dialog chỉ đọc — hiển thị thông tin chi tiết một phòng và danh sách khách đang ở (không sửa DB)
	public partial class RoomDetailDialog : Window
	{
		public RoomDetailDialog(RoomDetailDto detail)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// tiêu đề và các nhãn thông tin tĩnh từ DTO do LandlordRoomService.GetDetail trả về
			lbTitle.Text = $"Chi tiết phòng {detail.RoomNumber}";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbProperty.Text = detail.PropertyName;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbBuilding.Text = detail.BuildingName;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbRoomNumber.Text = detail.RoomNumber;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbRoomType.Text = detail.RoomTypeName;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbFloor.Text = detail.Floor.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbArea.Text = detail.Area.HasValue
				// Execute UI step inside RoomDetailDialog
				? $"{detail.Area.Value.ToString("N1", CultureInfo.CurrentCulture)} m²"
				// Execute UI step inside RoomDetailDialog
				: "-";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbBaseRent.Text = $"{detail.BaseRent.ToString("N0", CultureInfo.CurrentCulture)} đ";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbStatus.Text = detail.StatusDisplay;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbOccupancy.Text = $"{detail.Tenants.Count}/{detail.MaxOccupancy}";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbNotes.Text = string.IsNullOrWhiteSpace(detail.Notes) ? "-" : detail.Notes;

			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgTenants.ItemsSource = detail.Tenants;
			// Assign local/page state inside RoomDetailDialog without altering business rules
			bool empty = detail.Tenants.Count == 0;
			// hiện placeholder khi phòng chưa có khách
			brdNoTenants.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
		}

		// đóng dialog — DialogResult=true để caller biết người dùng đã xem xong
		private void btnClose_Click(object sender, RoutedEventArgs e) => DialogResult = true;
	}
}
