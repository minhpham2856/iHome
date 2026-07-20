using iHome.DAL.Repositories;
using System;
using BCrypt.Net;
using iHome.DAL.Entities;
using iHome.BLL.Common;
using iHome.BLL.DTOs.Landlord;

namespace iHome.BLL.Services
{
	public class AuthService
	{
		private readonly UserRepository _userRepository = new();
		private readonly AuditLogRepository _audits = new();
		private readonly EmailService _emailService = new();

		public AuthService() { }

		// main methods
		public User? Login(string username, string password)
		{
			var user = _userRepository.GetByUsername(username);

			// check if user exists
			if (user == null) return null;

			// check if password is correct
			bool isPasswordValid = VerifyPasswordHash(password, user.PasswordHash);
			if (!isPasswordValid)
			{
				TryAudit(user.Id, "LOGIN_FAILED", "Users", user.Id.ToString(), null, "Sai mật khẩu");
				return null;
			}

			if (!user.IsActive)
			{
				TryAudit(user.Id, "LOGIN_FAILED", "Users", user.Id.ToString(), null, "Tài khoản ngừng hoạt động");
				return null;
			}

			TryAudit(user.Id, "LOGIN", "Users", user.Id.ToString(), null, $"LastLogin={DateTime.Now:O}");
			return user;
		}

		public void Logout(int userId)
		{
			TryAudit(userId, "LOGOUT", "Users", userId.ToString(), null, null);
		}

		public bool Register(User user)
		{
			if (string.IsNullOrWhiteSpace(user.Username))
			{
				throw new ArgumentException("Tên đăng nhập không được để trống.");
			}
			EnsureUniqueUsername(user.Username);
			EnsureUniqueEmail(user.Email);

			var newUser = new User
			{
				FullName = InputFormatter.FormatFullName(user.FullName),
				Username = user.Username.Trim(),
				PasswordHash = HashPassword(IdentityGenerator.GeneratePassword()),
				Email = user.Email.Trim(),
				PhoneNumber = user.PhoneNumber,
				IsActive = true,
				CreatedAt = DateTime.Now,
				Role = user.Role
			};

			if (!_userRepository.Add(newUser))
			{
				return false;
			}

			TryAudit(newUser.Id, "REGISTER", "Users", newUser.Id.ToString(), null,
				$"Username={newUser.Username}; Role={newUser.Role}");
			return true;
		}

		public bool ChangePassword(string username, string oldPassword, string newPassword)
		{
			var user = _userRepository.GetByUsername(username);
			if (user == null) return false;

			bool isOldPasswordValid = VerifyPasswordHash(oldPassword, user.PasswordHash);
			if (!isOldPasswordValid) return false;

			if (!InputValidator.IsValidPassword(newPassword))
			{
				throw new ArgumentException("Mật khẩu mới phải 8-20 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
			}

			if (!_userRepository.UpdatePassword(user.Id, HashPassword(newPassword)))
			{
				return false;
			}

			TryAudit(user.Id, "CHANGE_PASSWORD", "Users", user.Id.ToString(), null, "Đã đổi mật khẩu");
			return true;
		}

		// đặt lại mật khẩu theo email và gửi mật khẩu tạm
		public void ForgotPassword(string email)
		{
			if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}

			var user = _userRepository.GetByEmail(email.Trim());
			if (user == null || !user.IsActive)
			{
				throw new ArgumentException("Không tìm thấy tài khoản với email này.");
			}

			string password = IdentityGenerator.GeneratePassword();
			if (!_userRepository.UpdatePassword(user.Id, HashPassword(password)))
			{
				throw new InvalidOperationException("Không thể đặt lại mật khẩu.");
			}

			TryAudit(user.Id, "FORGOT_PASSWORD", "Users", user.Id.ToString(), null, "Đã gửi mật khẩu tạm qua email");

			_emailService.SendPasswordResetInfo(user.Email, password);
		}

		public bool UpdateProfile(int userId, string username, string fullName, string email, string? phone)
		{
			if (string.IsNullOrWhiteSpace(username) || !InputValidator.IsValidUsername(username.Trim()))
			{
				throw new ArgumentException("Tên đăng nhập 3-20 ký tự, chỉ gồm chữ, số, dấu chấm hoặc gạch dưới.");
			}
			if (string.IsNullOrWhiteSpace(fullName))
			{
				throw new ArgumentException("Họ tên không được để trống.");
			}
			if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}

			var user = _userRepository.GetById(userId);
			if (user == null) return false;

			string normalizedUsername = username.Trim();
			string normalizedEmail = email.Trim();
			EnsureUniqueUsername(normalizedUsername, userId);
			EnsureUniqueEmail(normalizedEmail, userId);

			string oldValue =
				$"Username={user.Username}; Name={user.FullName}; Email={user.Email}; Phone={user.PhoneNumber}";

			user.Username = normalizedUsername;
			user.FullName = InputFormatter.FormatFullName(fullName);
			user.Email = normalizedEmail;
			user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
			if (!_userRepository.Update(user))
			{
				return false;
			}

			TryAudit(userId, "UPDATE_PROFILE", "Users", userId.ToString(), oldValue,
				$"Username={user.Username}; Name={user.FullName}; Email={user.Email}; Phone={user.PhoneNumber}");
			return true;
		}

		public User? GetById(int userId) => _userRepository.GetById(userId);

		// tạo tài khoản Manager: username theo họ tên + mật khẩu tạm + gửi email; gắn 1 nhà trọ
		public ManagerAccountCreatedDto CreateManagerAccount(
			string fullName,
			string email,
			string? phone,
			int managedPropertyId)
		{
			if (string.IsNullOrWhiteSpace(fullName))
			{
				throw new ArgumentException("Họ tên không được để trống.");
			}
			if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}
			if (managedPropertyId <= 0)
			{
				throw new ArgumentException("Chọn nhà trọ để phân công.");
			}

			string normalizedEmail = email.Trim();
			EnsureUniqueEmail(normalizedEmail);

			string formattedName = InputFormatter.FormatFullName(fullName);
			string username = GenerateUniqueUsername(formattedName);
			string password = IdentityGenerator.GeneratePassword();

			var newUser = new User
			{
				FullName = formattedName,
				Username = username,
				PasswordHash = HashPassword(password),
				Email = normalizedEmail,
				PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
				IsActive = true,
				CreatedAt = DateTime.Now,
				Role = "Manager",
				ManagedPropertyId = managedPropertyId
			};

			if (!_userRepository.Add(newUser))
			{
				throw new InvalidOperationException("Không thể tạo tài khoản nhân viên.");
			}

			var saved = _userRepository.GetByUsername(username)
				?? throw new InvalidOperationException("Không thể tải tài khoản vừa tạo.");

			var result = new ManagerAccountCreatedDto
			{
				UserId = saved.Id,
				Username = username,
				TemporaryPassword = password,
				EmailSent = false
			};

			try
			{
				_emailService.SendLoginInfo(saved.Email, username, password);
				result.EmailSent = true;
			}
			catch (Exception ex)
			{
				result.EmailSent = false;
				result.EmailError = ex.Message;
			}

			return result;
		}

		public void EnsureUniqueUsername(string username, int? excludeUserId = null)
		{
			if (_userRepository.UsernameExists(username, excludeUserId))
			{
				throw new ArgumentException("Tên đăng nhập đã được sử dụng.");
			}
		}

		public void EnsureUniqueEmail(string email, int? excludeUserId = null)
		{
			if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}
			if (_userRepository.EmailExists(email, excludeUserId))
			{
				throw new ArgumentException("Email đã được sử dụng.");
			}
		}

		private string GenerateUniqueUsername(string fullName)
		{
			for (int attempt = 0; attempt < 20; attempt++)
			{
				string username = IdentityGenerator.GenerateUsername(fullName);
				if (username.Length > 20)
				{
					// giữ đuôi 6 số, cắt phần tên nếu vượt MaxLength username
					string suffix = username[^6..];
					string prefix = username[..^6];
					if (prefix.Length > 14) prefix = prefix[..14];
					username = prefix + suffix;
				}
				if (!_userRepository.UsernameExists(username))
				{
					return username;
				}
			}
			throw new InvalidOperationException("Không thể tạo tên đăng nhập duy nhất. Thử lại.");
		}

		// ghi nhật ký
		private void TryAudit(
			int userId,
			string action,
			string tableName,
			string? recordId,
			string? oldValue,
			string? newValue)
		{
			try
			{
				_audits.Add(userId, action, tableName, recordId, oldValue, newValue);
			}
			catch
			{
			}
		}

		// helper methods
		private bool VerifyPasswordHash(string password, string storedHash)
		{
			return BCrypt.Net.BCrypt.Verify(password, storedHash);
		}

		private string HashPassword(string password)
		{
			return BCrypt.Net.BCrypt.HashPassword(password);
		}
	}
}
