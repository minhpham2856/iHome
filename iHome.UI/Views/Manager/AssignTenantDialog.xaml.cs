using iHome.BLL.DTOs.Manager;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	// Code-behind for the AssignTenantDialog modal dialog.
	public partial class AssignTenantDialog : Window
	{
		public int ContractId { get; private set; }
		public int TenantId { get; private set; }
		public bool IsMainTenant { get; private set; }

		public AssignTenantDialog(
			IEnumerable<ContractOptionDto> contracts,
			IEnumerable<LookupOptionDto> tenants,
			int? selectedTenantId = null,
			int? selectedContractId = null)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbContract.ItemsSource = contracts.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbTenant.ItemsSource = tenants.ToList();
			// Restore or set combo selection to match entity id or filter
			cbContract.SelectedItem = selectedContractId.HasValue
				// Work with BLL DTO/form object returned from service or built from controls
				? cbContract.Items.Cast<ContractOptionDto>().FirstOrDefault(item => item.Id == selectedContractId.Value)
				// Execute UI step inside AssignTenantDialog
				: null;
			// Restore or set combo selection to match entity id or filter
			if (cbContract.SelectedItem == null)
			{
				// Pick default combo index (usually first/all option) after reload
				cbContract.SelectedIndex = cbContract.Items.Count > 0 ? 0 : -1;
			}
			// Restore or set combo selection to match entity id or filter
			cbTenant.SelectedItem = selectedTenantId.HasValue
				// Work with BLL DTO/form object returned from service or built from controls
				? cbTenant.Items.Cast<LookupOptionDto>().FirstOrDefault(item => item.Id == selectedTenantId.Value)
				// Execute UI step inside AssignTenantDialog
				: null;
			// Restore or set combo selection to match entity id or filter
			if (cbTenant.SelectedItem == null)
			{
				// Pick default combo index (usually first/all option) after reload
				cbTenant.SelectedIndex = cbTenant.Items.Count > 0 ? 0 : -1;
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (cbContract.SelectedItem is not ContractOptionDto contract ||
				// Change combo selection to drive filter cascade or dialog default
				cbTenant.SelectedItem is not LookupOptionDto tenant)
			{
				// ManagerUi.ShowValidation shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowValidation("Không có hợp đồng hoặc khách phù hợp.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Assign local/page state inside Save_Click without altering business rules
			ContractId = contract.Id;
			// Assign local/page state inside Save_Click without altering business rules
			TenantId = tenant.Id;
			// Read CheckBox to capture boolean flag such as main-tenant selection
			IsMainTenant = chkMainTenant.IsChecked == true;
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private void cbContract_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; cbTenant.Focus(); }
		}

		private void cbTenant_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; chkMainTenant.Focus(); }
		}
	}
}
