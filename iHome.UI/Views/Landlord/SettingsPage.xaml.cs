using iHome.BLL.Services;
using iHome.DAL.Entities;
using iHome.UI.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// trang cài đặt Landlord — sửa hồ sơ và đổi mật khẩu qua AuthService; đồng bộ lời chào DashboardWindow
	public partial class SettingsPage : Page
	{
		private readonly User _currentUser;
		private readonly AuthService _authService = new();

		public SettingsPage(User user)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			// Call helper LoadProfile to refresh UI state from BLL data
			LoadProfile();
		}

		// nạp form từ DB (fresh) — txtRole read-only hiển thị Role string
		private void LoadProfile()
		{
			// Call AuthService.GetById for login, profile, or password operations
			var user = _authService.GetById(_currentUser.Id) ?? _currentUser;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtUsername.Text = user.Username;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtRole.Text = user.Role;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtFullName.Text = user.FullName;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtEmail.Text = user.Email;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtPhone.Text = user.PhoneNumber ?? string.Empty;
		}

		// lưu hồ sơ — cập nhật entity _currentUser in-memory và RefreshWelcome trên shell
		private void btnSaveProfile_Click(object sender, RoutedEventArgs e)
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call AuthService.UpdateProfile for login, profile, or password operations
				bool ok = _authService.UpdateProfile(new User
				{
					// Assign local/page state inside btnSaveProfile_Click without altering business rules
					Id = _currentUser.Id,
					// Update TextBlock/TextBox caption or read user-entered text from control
					Username = txtUsername.Text,
					// Update TextBlock/TextBox caption or read user-entered text from control
					FullName = txtFullName.Text,
					// Update TextBlock/TextBox caption or read user-entered text from control
					Email = txtEmail.Text,
					// Update TextBlock/TextBox caption or read user-entered text from control
					PhoneNumber = txtPhone.Text
				// Execute UI step inside btnSaveProfile_Click
				});

				// Guard clause: only continue when UI selection, role, or input is valid
				if (!ok)
				{
					// Show Vietnamese MessageBox to confirm, warn, or report success/failure
					MessageBox.Show("Không thể cập nhật thông tin.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
					// Exit method early or return value/tuple to caller
					return;
				}

				// Call AuthService.GetById for login, profile, or password operations
				var updated = _authService.GetById(_currentUser.Id);
				// Guard clause: only continue when UI selection, role, or input is valid
				if (updated != null)
				{
					// Assign local/page state inside btnSaveProfile_Click without altering business rules
					_currentUser.Username = updated.Username;
					// Assign local/page state inside btnSaveProfile_Click without altering business rules
					_currentUser.FullName = updated.FullName;
					// Assign local/page state inside btnSaveProfile_Click without altering business rules
					_currentUser.Email = updated.Email;
					// Assign local/page state inside btnSaveProfile_Click without altering business rules
					_currentUser.PhoneNumber = updated.PhoneNumber;
				}

				// Resolve parent Window so modal dialogs center on the app shell
				if (Window.GetWindow(this) is DashboardWindow dashboard)
				{
					// Execute UI step inside btnSaveProfile_Click
					dashboard.RefreshWelcome();
				}

				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã lưu thông tin cá nhân.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
				// Call helper LoadProfile to refresh UI state from BLL data
				LoadProfile();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (ArgumentException ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Không thể cập nhật thông tin.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// đổi mật khẩu — verify cũ qua BCrypt trong AuthService; xóa field sau thành công
		private void btnChangePassword_Click(object sender, RoutedEventArgs e)
		{
			// Read or clear PasswordBox secure text for auth/change-password forms
			if (txtNewPassword.Password != txtConfirmPassword.Password)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Xác nhận mật khẩu mới không khớp.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call AuthService.ChangePassword for login, profile, or password operations
				bool ok = _authService.ChangePassword(
					// Execute UI step inside btnChangePassword_Click
					_currentUser,
					// Read or clear PasswordBox secure text for auth/change-password forms
					txtOldPassword.Password,
					// Read or clear PasswordBox secure text for auth/change-password forms
					txtNewPassword.Password);

				// Guard clause: only continue when UI selection, role, or input is valid
				if (!ok)
				{
					// Show Vietnamese MessageBox to confirm, warn, or report success/failure
					MessageBox.Show("Mật khẩu hiện tại không đúng.", "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
					// Exit method early or return value/tuple to caller
					return;
				}

				// Read or clear PasswordBox secure text for auth/change-password forms
				txtOldPassword.Password = string.Empty;
				// Read or clear PasswordBox secure text for auth/change-password forms
				txtNewPassword.Password = string.Empty;
				// Read or clear PasswordBox secure text for auth/change-password forms
				txtConfirmPassword.Password = string.Empty;
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã đổi mật khẩu thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (ArgumentException ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Không thể đổi mật khẩu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Enter — tab order các field hồ sơ
		private void txtUsername_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtFullName.Focus(); }
		}

		private void txtRole_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtFullName.Focus(); }
		}

		private void txtFullName_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtEmail.Focus(); }
		}

		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtPhone.Focus(); }
		}

		private void txtPhone_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; btnSaveProfile.Focus(); }
		}

		// Enter — tab order các field đổi mật khẩu
		private void txtOldPassword_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtNewPassword.Focus(); }
		}

		private void txtNewPassword_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtConfirmPassword.Focus(); }
		}

		private void txtConfirmPassword_KeyDown(object sender, KeyEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (e.Key == Key.Enter)
			{
				// Consume Enter key so WPF does not trigger default button twice
				e.Handled = true;
				// Handle Enter key to move focus or submit like clicking the primary button
				btnChangePassword_Click(btnChangePassword, new RoutedEventArgs());
			}
		}
	}
}
