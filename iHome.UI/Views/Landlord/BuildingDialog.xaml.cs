using iHome.BLL.DTOs.Landlord;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	public partial class BuildingDialog : Window
	{
		private readonly int _buildingId;
		private readonly int _propertyId;
		public BuildingFormDto? Result { get; private set; }

		public BuildingDialog(int propertyId, List<ManagerOptionDto> managers, BuildingFormDto? building = null)
		{
			InitializeComponent();
			_propertyId = propertyId;
			_buildingId = building?.Id ?? 0;
			cbManager.ItemsSource = managers;

			if (building == null)
			{
				txtFloors.Text = "1";
				cbManager.SelectedIndex = 0;
				return;
			}

			lblTitle.Text = "Cập nhật tòa nhà";
			txtName.Text = building.Name;
			txtFloors.Text = building.NumberOfFloors.ToString();
			txtDescription.Text = building.Description ?? string.Empty;
			chkIsActive.IsChecked = building.IsActive;
			cbManager.SelectedValue = building.ManagerId;
			if (cbManager.SelectedItem == null)
			{
				cbManager.SelectedIndex = 0;
			}
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtName.Text))
			{
				MessageBox.Show("Tên tòa nhà không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (!int.TryParse(txtFloors.Text.Trim(), out int floors) || floors < 1)
			{
				MessageBox.Show("Số tầng phải lớn hơn hoặc bằng 1.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Result = new BuildingFormDto
			{
				Id = _buildingId,
				PropertyId = _propertyId,
				Name = txtName.Text,
				NumberOfFloors = floors,
				Description = txtDescription.Text,
				ManagerId = (cbManager.SelectedItem as ManagerOptionDto)?.Id,
				IsActive = chkIsActive.IsChecked == true
			};
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// key down enter
		private void txtName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtFloors.Focus(); }
		}

		private void txtFloors_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cbManager.Focus(); }
		}

		private void cbManager_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; chkIsActive.Focus(); }
		}
	}
}
