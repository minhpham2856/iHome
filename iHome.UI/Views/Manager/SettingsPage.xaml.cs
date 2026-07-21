using iHome.BLL.Services;
using iHome.DAL.Entities;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	// Trang cài đặt tài khoản Manager: cập nhật hồ sơ + đổi mật khẩu (dùng AuthService chung với Landlord)
	public partial class SettingsPage : Page
	{
		private readonly User _currentUser;
		private readonly AuthService _authService = new();

		public SettingsPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			LoadProfile();
		}

		// Nạp thông tin user hiện tại lên form; Role hiển thị tiếng Việt
		private void LoadProfile()
		{
			var user = _authService.GetById(_currentUser.Id) ?? _currentUser;
			txtUsername.Text = user.Username;
			txtRole.Text = user.Role == "Manager" ? "Quản lý" : user.Role;
			txtFullName.Text = user.FullName;
			txtEmail.Text = user.Email;
			txtPhone.Text = user.PhoneNumber ?? string.Empty;
		}

		// Lưu hồ sơ rồi đồng bộ session + lời chào trên DashboardWindow
		private void btnSaveProfile_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				bool ok = _authService.UpdateProfile(
					_currentUser.Id,
					txtUsername.Text,
					txtFullName.Text,
					txtEmail.Text,
					txtPhone.Text);

				if (!ok)
				{
					ManagerUi.ShowError("Không thể cập nhật thông tin.");
					return;
				}

				var updated = _authService.GetById(_currentUser.Id);
				if (updated != null)
				{
					_currentUser.Username = updated.Username;
					_currentUser.FullName = updated.FullName;
					_currentUser.Email = updated.Email;
					_currentUser.PhoneNumber = updated.PhoneNumber;
				}

				// Cập nhật "Xin chào, ..." trên header ngay sau khi đổi họ tên
				if (Window.GetWindow(this) is DashboardWindow dashboard)
				{
					dashboard.RefreshWelcome();
				}

				MessageBox.Show("Đã lưu thông tin cá nhân.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
				LoadProfile();
			}
			catch (ArgumentException ex)
			{
				ManagerUi.ShowValidation(ex.Message);
			}
			catch (Exception)
			{
				ManagerUi.ShowError("Không thể cập nhật thông tin.");
			}
		}

		// Đổi mật khẩu: xác nhận khớp trước, sau đó AuthService kiểm tra mật khẩu cũ
		private void btnChangePassword_Click(object sender, RoutedEventArgs e)
		{
			if (txtNewPassword.Password != txtConfirmPassword.Password)
			{
				ManagerUi.ShowValidation("Xác nhận mật khẩu mới không khớp.");
				return;
			}

			try
			{
				bool ok = _authService.ChangePassword(
					_currentUser.Username,
					txtOldPassword.Password,
					txtNewPassword.Password);

				if (!ok)
				{
					ManagerUi.ShowError("Mật khẩu hiện tại không đúng.");
					return;
				}

				txtOldPassword.Password = string.Empty;
				txtNewPassword.Password = string.Empty;
				txtConfirmPassword.Password = string.Empty;
				MessageBox.Show("Đã đổi mật khẩu thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (ArgumentException ex)
			{
				ManagerUi.ShowValidation(ex.Message);
			}
			catch (Exception)
			{
				ManagerUi.ShowError("Không thể đổi mật khẩu.");
			}
		}

		// Enter để nhảy ô tiếp theo trên form hồ sơ / mật khẩu
		private void txtUsername_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtFullName.Focus(); }
		}

		private void txtRole_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtFullName.Focus(); }
		}

		private void txtFullName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtEmail.Focus(); }
		}

		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtPhone.Focus(); }
		}

		private void txtPhone_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; btnSaveProfile.Focus(); }
		}

		private void txtOldPassword_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtNewPassword.Focus(); }
		}

		private void txtNewPassword_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtConfirmPassword.Focus(); }
		}

		private void txtConfirmPassword_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				btnChangePassword_Click(btnChangePassword, new RoutedEventArgs());
			}
		}
	}
}
