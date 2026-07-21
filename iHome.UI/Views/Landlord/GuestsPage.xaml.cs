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
	public partial class GuestsPage : Page
	{
		private readonly User _currentUser;
		private readonly IHomeDbContext _db;
		private readonly LandlordTenantService _service;
		private List<TenantDto> _tenants = new();

		public GuestsPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			_db = new IHomeDbContext();
			_service = new LandlordTenantService(_db);
			Unloaded += GuestsPage_Unloaded;
			LoadTenants();
		}

		private void GuestsPage_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= GuestsPage_Unloaded;
			_db.Dispose();
		}

		private TenantDto? SelectedTenant => dgTenants.SelectedItem as TenantDto;

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

		private void ApplyFilter(int? keepSelectedId = null)
		{
			string keyword = (txtSearch?.Text ?? string.Empty).Trim();
			var filtered = string.IsNullOrEmpty(keyword)
				? _tenants
				: _tenants.Where(t =>
					t.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| t.IdCardNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| t.PhoneNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| (t.Email?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
					|| t.CurrentBuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| t.CurrentRoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

			dgTenants.ItemsSource = filtered;
			brdTenantState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			txtTenantState.Text = string.IsNullOrEmpty(keyword)
				? "Chưa có khách thuê. Bấm + Thêm để tạo mới."
				: "Không tìm thấy khách phù hợp.";

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
				lblContractSection.Text = $"Hợp đồng - {tenant.FullName}";
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
			lblContractSection.Text = "Hợp đồng";
		}

		private void UpdateButtons()
		{
			bool hasSelection = SelectedTenant != null;
			btnEdit.IsEnabled = hasSelection;
			btnDelete.IsEnabled = hasSelection;
		}

		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) =>
			ApplyFilter(SelectedTenant?.Id);

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadTenants(SelectedTenant?.Id);

		private void dgTenants_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateButtons();
			LoadContracts();
		}

		private void dgTenants_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (SelectedTenant != null) OpenEdit();
		}

		private void btnAdd_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new TenantDialog { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() != true || dialog.Result == null) return;

			try
			{
				int id = _service.Create(_currentUser.Id, dialog.Result);
				MessageBox.Show("Đã thêm khách thuê.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
				LoadTenants(id);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEdit_Click(object sender, RoutedEventArgs e) => OpenEdit();

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

		private void btnDelete_Click(object sender, RoutedEventArgs e)
		{
			var selected = SelectedTenant;
			if (selected == null) return;

			var confirm = MessageBox.Show(
				$"Xóa khách thuê \"{selected.FullName}\"?\nChỉ xóa được khi khách chưa có hợp đồng.",
				"Xác nhận xóa",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);
			if (confirm != MessageBoxResult.Yes) return;

			try
			{
				_service.Delete(_currentUser.Id, selected.Id);
				MessageBox.Show("Đã xóa khách thuê.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
				LoadTenants();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}
