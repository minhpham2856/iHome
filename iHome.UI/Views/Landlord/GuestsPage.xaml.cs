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
	// Tenants — master grid + contract detail; client-side property/building/search filters
	public partial class GuestsPage : Page
	{
		private readonly User _currentUser;
		private readonly IHomeDbContext _db;
		private readonly LandlordTenantService _service;
		private List<TenantDto> _tenants = new();
		private bool _suppressFilterEvents;

		public GuestsPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			_db = new IHomeDbContext();
			_service = new LandlordTenantService(_db);
			Unloaded += GuestsPage_Unloaded;
			LoadPropertyFilter();
		}

		private void GuestsPage_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= GuestsPage_Unloaded;
			_db.Dispose();
		}

		private PropertyFilterOptionDto? SelectedProperty => cbProperty.SelectedItem as PropertyFilterOptionDto;
		private BuildingFilterOptionDto? SelectedBuilding => cbBuilding.SelectedItem as BuildingFilterOptionDto;
		private TenantDto? SelectedTenant => dgTenants.SelectedItem as TenantDto;

		// Cascade: property combo → building combo → LoadTenants
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
				LoadTenants();
			}
			catch (Exception)
			{
				_suppressFilterEvents = false;
				MessageBox.Show("Không thể tải danh sách tòa nhà.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Load full list from BLL then ApplyFilter in memory
		private void LoadTenants(int? keepSelectedId = null)
		{
			try
			{
				_tenants = _service.GetByLandlord(_currentUser.Id);
				ApplyFilter(keepSelectedId);
			}
			catch (Exception)
			{
				MessageBox.Show("Không thể tải danh sách khách thuê.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Filter by property/building/keyword; update empty state and selection
		private void ApplyFilter(int? keepSelectedId = null)
		{
			int propertyId = SelectedProperty?.Id ?? 0;
			int buildingId = SelectedBuilding?.Id ?? 0;
			string keyword = (txtSearch?.Text ?? string.Empty).Trim();

			IEnumerable<TenantDto> query = _tenants;
			if (propertyId > 0)
			{
				query = query.Where(t => t.CurrentPropertyId == propertyId);
			}
			if (buildingId > 0)
			{
				query = query.Where(t => t.CurrentBuildingId == buildingId);
			}
			if (!string.IsNullOrEmpty(keyword))
			{
				query = query.Where(t =>
					t.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| t.IdCardNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| t.PhoneNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| (t.Email?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
					|| t.CurrentBuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| t.CurrentRoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase));
			}

			var filtered = query.ToList();
			dgTenants.ItemsSource = filtered;
			brdTenantState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			lbTenantState.Text = !string.IsNullOrEmpty(keyword) || propertyId > 0 || buildingId > 0
				? "Không tìm thấy khách phù hợp."
				: "Chưa có khách thuê trong hệ thống nhà trọ của bạn.";

			if (keepSelectedId.HasValue)
			{
				dgTenants.SelectedItem = filtered.FirstOrDefault(t => t.Id == keepSelectedId.Value);
			}

			if (dgTenants.SelectedItem == null)
			{
				ClearContracts();
			}

			UpdateButtons();
		}

		// Right panel — contracts for the selected tenant
		private void LoadContracts()
		{
			var tenant = SelectedTenant;
			if (tenant == null)
			{
				ClearContracts();
				return;
			}

			try
			{
				var contracts = _service.GetContracts(_currentUser.Id, tenant.Id);
				dgContracts.ItemsSource = contracts;
				brdContractState.Visibility = contracts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				if (contracts.Count == 0)
				{
					((TextBlock)brdContractState.Child).Text = "Khách này chưa có hợp đồng trong hệ thống nhà trọ của bạn.";
				}
				lbContractSection.Text = $"Hợp đồng - {tenant.FullName}";
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				ClearContracts();
			}
		}

		private void ClearContracts()
		{
			dgContracts.ItemsSource = null;
			brdContractState.Visibility = Visibility.Visible;
			((TextBlock)brdContractState.Child).Text = "Chọn một khách để xem hợp đồng và phòng đang thuê.";
			lbContractSection.Text = "Hợp đồng";
		}

		private void UpdateButtons() =>
			btnEdit.IsEnabled = SelectedTenant != null;

		private void cbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressFilterEvents) return;
			LoadBuildingFilter();
		}

		private void cbBuilding_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressFilterEvents) return;
			ApplyFilter(SelectedTenant?.Id);
		}

		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) =>
			ApplyFilter(SelectedTenant?.Id);

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadPropertyFilter(SelectedProperty?.Id, SelectedBuilding?.Id);

		private void dgTenants_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateButtons();
			LoadContracts();
		}

		private void dgTenants_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (SelectedTenant != null) OpenEdit();
		}

		private void btnEdit_Click(object sender, RoutedEventArgs e) => OpenEdit();

		// Open TenantDialog — update only (no create from this page)
		private void OpenEdit()
		{
			var selected = SelectedTenant;
			if (selected == null) return;

			try
			{
				var form = _service.GetForm(_currentUser.Id, selected.Id);
				if (form == null)
				{
					MessageBox.Show("Không tìm thấy khách thuê.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				var dialog = new TenantDialog(form) { Owner = Window.GetWindow(this) };
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				_service.Update(_currentUser.Id, dialog.Result);
				MessageBox.Show("Đã cập nhật khách thuê.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
				LoadTenants(selected.Id);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}
