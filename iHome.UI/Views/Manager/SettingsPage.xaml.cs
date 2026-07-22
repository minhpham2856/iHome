using iHome.BLL.Services;
using iHome.DAL.Entities;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	// Trang cài đặt tài khoản Manager: cập nhật hồ sơ + đổi mật khẩu
	public partial class SettingsPage : Page
	{
		private readonly User _currentUser;
		private readonly AuthService _authService = new();

		// Nạp hồ sơ user hiện tại
		public SettingsPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			LoadProfile();
		}

		// Nạp thông tin user; Role hiển thị tiếng Việt
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
				bool ok = _authService.UpdateProfile(new User
				{
					Id = _currentUser.Id,
					Username = txtUsername.Text,
					FullName = txtFullName.Text,
					Email = txtEmail.Text,
					PhoneNumber = txtPhone.Text
				});

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

		// Đổi mật khẩu: xác nhận khớp trước, AuthService kiểm tra mật khẩu cũ
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
					_currentUser,
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

		// Enter → họ tên
		private void txtUsername_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtFullName.Focus(); }
		}

		// Enter → họ tên (ô vai trò chỉ đọc)
		private void txtRole_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtFullName.Focus(); }
		}

		// Enter → email
		private void txtFullName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtEmail.Focus(); }
		}

		// Enter → SĐT
		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtPhone.Focus(); }
		}

		// Enter → nút lưu hồ sơ
		private void txtPhone_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; btnSaveProfile.Focus(); }
		}

		// Enter → mật khẩu mới
		private void txtOldPassword_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtNewPassword.Focus(); }
		}

		// Enter → xác nhận mật khẩu
		private void txtNewPassword_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtConfirmPassword.Focus(); }
		}

		// Enter trên xác nhận → đổi mật khẩu
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
