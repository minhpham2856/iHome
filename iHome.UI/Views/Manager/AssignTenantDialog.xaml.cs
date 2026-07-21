using iHome.BLL.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	public partial class AssignTenantDialog : Window
	{
		public int ContractId { get; private set; }
		public int TenantId { get; private set; }
		public bool IsMainTenant { get; private set; }

		public AssignTenantDialog(
			IEnumerable<ManagerContractOptionDto> contracts,
			IEnumerable<ManagerLookupOptionDto> tenants,
			int? selectedTenantId = null,
			int? selectedContractId = null)
		{
			InitializeComponent();
			CbContract.ItemsSource = contracts.ToList();
			CbTenant.ItemsSource = tenants.ToList();
			CbContract.SelectedItem = selectedContractId.HasValue
				? CbContract.Items.Cast<ManagerContractOptionDto>().FirstOrDefault(item => item.Id == selectedContractId.Value)
				: null;
			if (CbContract.SelectedItem == null)
			{
				CbContract.SelectedIndex = CbContract.Items.Count > 0 ? 0 : -1;
			}
			CbTenant.SelectedItem = selectedTenantId.HasValue
				? CbTenant.Items.Cast<ManagerLookupOptionDto>().FirstOrDefault(item => item.Id == selectedTenantId.Value)
				: null;
			if (CbTenant.SelectedItem == null)
			{
				CbTenant.SelectedIndex = CbTenant.Items.Count > 0 ? 0 : -1;
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (CbContract.SelectedItem is not ManagerContractOptionDto contract ||
				CbTenant.SelectedItem is not ManagerLookupOptionDto tenant)
			{
				ManagerUi.ShowValidation("Không có hợp đồng hoặc khách phù hợp.");
				return;
			}
			ContractId = contract.Id;
			TenantId = tenant.Id;
			IsMainTenant = ChkMainTenant.IsChecked == true;
			DialogResult = true;
		}

		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// key down enter
		private void CbContract_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; CbTenant.Focus(); }
		}

		private void CbTenant_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; ChkMainTenant.Focus(); }
		}
	}
}
