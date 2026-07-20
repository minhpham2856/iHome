using iHome.BLL.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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
			CboContract.ItemsSource = contracts.ToList();
			CboTenant.ItemsSource = tenants.ToList();
			CboContract.SelectedItem = selectedContractId.HasValue
				? CboContract.Items.Cast<ManagerContractOptionDto>().FirstOrDefault(item => item.Id == selectedContractId.Value)
				: null;
			if (CboContract.SelectedItem == null)
			{
				CboContract.SelectedIndex = CboContract.Items.Count > 0 ? 0 : -1;
			}
			CboTenant.SelectedItem = selectedTenantId.HasValue
				? CboTenant.Items.Cast<ManagerLookupOptionDto>().FirstOrDefault(item => item.Id == selectedTenantId.Value)
				: null;
			if (CboTenant.SelectedItem == null)
			{
				CboTenant.SelectedIndex = CboTenant.Items.Count > 0 ? 0 : -1;
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (CboContract.SelectedItem is not ManagerContractOptionDto contract ||
				CboTenant.SelectedItem is not ManagerLookupOptionDto tenant)
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
	}
}
