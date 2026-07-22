using iHome.BLL.DTOs.Manager;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	// Dialog gắn khách vào hợp đồng (chính hoặc phụ)
	public partial class AssignTenantDialog : Window
	{
		public int ContractId { get; private set; }
		public int TenantId { get; private set; }
		public bool IsMainTenant { get; private set; }

		// Nạp combo; có thể chọn sẵn khách hoặc hợp đồng
		public AssignTenantDialog(
			IEnumerable<ContractOptionDto> contracts,
			IEnumerable<LookupOptionDto> tenants,
			int? selectedTenantId = null,
			int? selectedContractId = null)
		{
			InitializeComponent();
			cbContract.ItemsSource = contracts.ToList();
			cbTenant.ItemsSource = tenants.ToList();
			cbContract.SelectedItem = selectedContractId.HasValue
				? cbContract.Items.Cast<ContractOptionDto>().FirstOrDefault(item => item.Id == selectedContractId.Value)
				: null;
			if (cbContract.SelectedItem == null)
			{
				cbContract.SelectedIndex = cbContract.Items.Count > 0 ? 0 : -1;
			}
			cbTenant.SelectedItem = selectedTenantId.HasValue
				? cbTenant.Items.Cast<LookupOptionDto>().FirstOrDefault(item => item.Id == selectedTenantId.Value)
				: null;
			if (cbTenant.SelectedItem == null)
			{
				cbTenant.SelectedIndex = cbTenant.Items.Count > 0 ? 0 : -1;
			}
		}

		// Lưu ContractId / TenantId / IsMainTenant
		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (cbContract.SelectedItem is not ContractOptionDto contract ||
				cbTenant.SelectedItem is not LookupOptionDto tenant)
			{
				ManagerUi.ShowValidation("Không có hợp đồng hoặc khách phù hợp.");
				return;
			}
			ContractId = contract.Id;
			TenantId = tenant.Id;
			IsMainTenant = chkMainTenant.IsChecked == true;
			DialogResult = true;
		}

		// Đóng dialog không lưu
		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter → ô khách
		private void cbContract_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cbTenant.Focus(); }
		}

		// Enter → checkbox người thuê chính
		private void cbTenant_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; chkMainTenant.Focus(); }
		}
	}
}
