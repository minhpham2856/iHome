using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Services;
using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// Contracts — master grid + tenants on contract; property/building/search filters
	public partial class ContractsPage : Page
	{
		private readonly User _currentUser;
		private readonly IHomeDbContext _db;
		private readonly LandlordContractService _service;
		private List<ContractDto> _contracts = new();
		private bool _suppressFilterEvents;

		public ContractsPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			_db = new IHomeDbContext();
			_service = new LandlordContractService(_db);
			Unloaded += ContractsPage_Unloaded;
			LoadPropertyFilter();
		}

		private void ContractsPage_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= ContractsPage_Unloaded;
			_db.Dispose();
		}

		private PropertyFilterOptionDto? SelectedProperty => cbProperty.SelectedItem as PropertyFilterOptionDto;
		private BuildingFilterOptionDto? SelectedBuilding => cbBuilding.SelectedItem as BuildingFilterOptionDto;
		private ContractDto? SelectedContract => dgContracts.SelectedItem as ContractDto;

		// Bind property combo then cascade buildings and contracts
		private void LoadPropertyFilter(int? keepPropertyId = null, int? keepBuildingId = null)
		{
			try
			{
				_suppressFilterEvents = true;
				var properties = _service.GetPropertyOptions(_currentUser.Id);
				cbProperty.ItemsSource = properties;
				if (keepPropertyId.HasValue)
				{
					cbProperty.SelectedItem = properties.FirstOrDefault(p => p.Id == keepPropertyId.Value);
				}
				else if (properties.Count > 0)
				{
					cbProperty.SelectedIndex = 0;
				}
				_suppressFilterEvents = false;
				LoadBuildingFilter(keepBuildingId);
			}
			catch (Exception)
			{
				_suppressFilterEvents = false;
				MessageBox.Show("Không thể tải danh sách nhà trọ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Bind building combo for selected property, then load contracts
		private void LoadBuildingFilter(int? keepBuildingId = null)
		{
			int propertyId = SelectedProperty?.Id ?? 0;
			try
			{
				_suppressFilterEvents = true;
				var buildings = _service.GetBuildingOptions(_currentUser.Id, propertyId);
				cbBuilding.ItemsSource = buildings;
				if (keepBuildingId.HasValue)
				{
					cbBuilding.SelectedItem = buildings.FirstOrDefault(b => b.Id == keepBuildingId.Value);
				}
				else if (buildings.Count > 0)
				{
					cbBuilding.SelectedIndex = 0;
				}
				_suppressFilterEvents = false;
				LoadContracts();
			}
			catch (Exception)
			{
				_suppressFilterEvents = false;
				MessageBox.Show("Không thể tải danh sách tòa nhà.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// BLL filters by property/building; keyword search is client-side in ApplyFilter
		private void LoadContracts(int? keepSelectedId = null)
		{
			try
			{
				int? propertyId = SelectedProperty?.Id is > 0 and int pid ? pid : null;
				int? buildingId = SelectedBuilding?.Id is > 0 and int bid ? bid : null;
				_contracts = _service.GetByLandlord(_currentUser.Id, propertyId, buildingId);
				ApplyFilter(keepSelectedId);
			}
			catch (Exception)
			{
				MessageBox.Show("Không thể tải danh sách hợp đồng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Client-side keyword filter over loaded contracts
		private void ApplyFilter(int? keepSelectedId = null)
		{
			string keyword = (txtSearch?.Text ?? string.Empty).Trim();
			var filtered = string.IsNullOrEmpty(keyword)
				? _contracts
				: _contracts.Where(c =>
					c.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| c.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| c.PropertyName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| c.MainTenantName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| c.StatusDisplay.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

			dgContracts.ItemsSource = filtered;
			brdContractState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			lbContractState.Text = string.IsNullOrEmpty(keyword)
				? "Chưa có hợp đồng trong phạm vi đã chọn."
				: "Không tìm thấy hợp đồng phù hợp.";

			if (keepSelectedId.HasValue)
			{
				dgContracts.SelectedItem = filtered.FirstOrDefault(c => c.Id == keepSelectedId.Value);
			}

			if (dgContracts.SelectedItem == null)
			{
				ClearTenants();
			}

			UpdateButtons();
		}

		// Right panel — tenants on the selected contract
		private void LoadTenants()
		{
			var contract = SelectedContract;
			if (contract == null)
			{
				ClearTenants();
				return;
			}

			try
			{
				var tenants = _service.GetTenants(_currentUser.Id, contract.Id);
				dgTenants.ItemsSource = tenants;
				brdTenantState.Visibility = tenants.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				if (tenants.Count == 0)
				{
					((TextBlock)brdTenantState.Child).Text = "Hợp đồng chưa có khách thuê.";
				}
				lbTenantSection.Text = $"Khách trên hợp đồng - Phòng {contract.RoomNumber}";
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				ClearTenants();
			}
		}

		private void ClearTenants()
		{
			dgTenants.ItemsSource = null;
			brdTenantState.Visibility = Visibility.Visible;
			((TextBlock)brdTenantState.Child).Text = "Chọn một hợp đồng để xem danh sách khách.";
			lbTenantSection.Text = "Khách trên hợp đồng";
		}

		private void UpdateButtons() =>
			btnEdit.IsEnabled = SelectedContract != null;

		private void cbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressFilterEvents) return;
			LoadBuildingFilter();
		}

		private void cbBuilding_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressFilterEvents) return;
			LoadContracts(SelectedContract?.Id);
		}

		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) =>
			ApplyFilter(SelectedContract?.Id);

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadPropertyFilter(SelectedProperty?.Id, SelectedBuilding?.Id);

		private void dgContracts_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateButtons();
			LoadTenants();
		}

		private void dgContracts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (SelectedContract != null) OpenEdit();
		}

		private void btnEdit_Click(object sender, RoutedEventArgs e) => OpenEdit();

		// Open ContractDialog for the selected row
		private void OpenEdit()
		{
			var selected = SelectedContract;
			if (selected == null) return;

			try
			{
				var form = _service.GetForm(_currentUser.Id, selected.Id);
				if (form == null)
				{
					MessageBox.Show("Không tìm thấy hợp đồng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				var dialog = new ContractDialog(form, _service.GetStatusOptions())
				{
					Owner = Window.GetWindow(this)
				};
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				_service.Update(_currentUser.Id, dialog.Result);
				MessageBox.Show("Đã cập nhật hợp đồng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
				LoadContracts(selected.Id);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}
