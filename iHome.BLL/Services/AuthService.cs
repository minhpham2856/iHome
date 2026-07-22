using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using iHome.DAL.Entities;
using iHome.BLL.Common;
using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Enums;

namespace iHome.BLL.Services
{
	// Authentication, profile updates, and Manager account creation; all auth flows write audit entries (TryAudit never blocks the caller)
	public class AuthService
	{
		private readonly UserRepository _userRepository = new();
		private readonly AuditLogRepository _audits = new();
		private readonly EmailService _emailService = new();

		public AuthService() { }

		// Login: verify username-or-email/password, check IsActive, audit success or failure
		public User? Login(string username, string password)
		{
			var user = _userRepository.GetByUsername(username)
				?? _userRepository.GetByEmail(username);
			if (user == null) return null;
			bool isPasswordValid = VerifyPasswordHash(password, user.PasswordHash);
			if (!isPasswordValid)
			{
				TryAudit(new AuditLog
				{
					UserId = user.Id,
					Action = AuditAction.LoginFailed,
					TableName = AuditObject.Users,
					RecordId = user.Id.ToString(),
					Detail = user.Username,
					OldValue = null,
					NewValue = AuditDetail.WrongPassword,
					Timestamp = DateTime.Now
				});
				return null;
			}
			if (!user.IsActive)
			{
				TryAudit(new AuditLog
				{
					UserId = user.Id,
					Action = AuditAction.LoginFailed,
					TableName = AuditObject.Users,
					RecordId = user.Id.ToString(),
					Detail = user.Username,
					OldValue = null,
					NewValue = AuditDetail.AccountInactive,
					Timestamp = DateTime.Now
				});
				return null;
			}
			TryAudit(new AuditLog
			{
				UserId = user.Id,
				Action = AuditAction.Login,
				TableName = AuditObject.Users,
				RecordId = user.Id.ToString(),
				Detail = user.Username,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.Status] = AuditDetail.LoginSuccess
				}),
				Timestamp = DateTime.Now
			});
			return user;
		}

		// Logout — audit only; session teardown is handled by the UI
		public void Logout(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			TryAudit(new AuditLog
			{
				UserId = user.Id,
				Action = AuditAction.Logout,
				TableName = AuditObject.Users,
				RecordId = user.Id.ToString(),
				Detail = user.Username,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.Status] = AuditDetail.Logout
				}),
				Timestamp = DateTime.Now
			});
		}

		// Change password: verify old → validate policy → hash → persist → audit
		public bool ChangePassword(User user, string oldPassword, string newPassword)
		{
			ArgumentNullException.ThrowIfNull(user);
			var existing = _userRepository.GetByUsername(user.Username);
			if (existing == null) return false;
			bool isOldPasswordValid = VerifyPasswordHash(oldPassword, existing.PasswordHash);
			if (!isOldPasswordValid) return false;
			if (!InputValidator.IsValidPassword(newPassword))
			{
				throw new ArgumentException("Mật khẩu mới phải 8-20 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
			}
			existing.PasswordHash = HashPassword(newPassword);
			if (!_userRepository.UpdatePassword(existing))
			{
				return false;
			}
			TryAudit(new AuditLog
			{
				UserId = existing.Id,
				Action = AuditAction.ChangePassword,
				TableName = AuditObject.Users,
				RecordId = existing.Id.ToString(),
				Detail = existing.Username,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.Status] = AuditDetail.PasswordChanged
				}),
				Timestamp = DateTime.Now
			});
			return true;
		}

		// Forgot password: lookup by email → generate temp password → hash/save → email plaintext password
		public void ForgotPassword(User lookup)
		{
			ArgumentNullException.ThrowIfNull(lookup);
			if (string.IsNullOrWhiteSpace(lookup.Email) || !lookup.Email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}
			var user = _userRepository.GetByEmail(lookup.Email.Trim());
			if (user == null || !user.IsActive)
			{
				throw new ArgumentException("Không tìm thấy tài khoản với email này.");
			}
			string password = IdentityGenerator.GeneratePassword();
			user.PasswordHash = HashPassword(password);
			if (!_userRepository.UpdatePassword(user))
			{
				throw new InvalidOperationException("Không thể đặt lại mật khẩu.");
			}
			TryAudit(new AuditLog
			{
				UserId = user.Id,
				Action = AuditAction.ForgotPassword,
				TableName = AuditObject.Users,
				RecordId = user.Id.ToString(),
				Detail = user.Username,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.Status] = AuditDetail.TempPasswordSent
				}),
				Timestamp = DateTime.Now
			});
			_emailService.SendPasswordResetInfo(user.Email, password);
		}

		// Update the signed-in user's profile — validate unique username/email, AuditDiff before/after
		public bool UpdateProfile(User profile)
		{
			ArgumentNullException.ThrowIfNull(profile);
			if (string.IsNullOrWhiteSpace(profile.Username) || !InputValidator.IsValidUsername(profile.Username.Trim()))
			{
				throw new ArgumentException("Tên đăng nhập 3-20 ký tự, chỉ gồm chữ, số, dấu chấm hoặc gạch dưới.");
			}
			if (string.IsNullOrWhiteSpace(profile.FullName))
			{
				throw new ArgumentException("Họ tên không được để trống.");
			}
			if (string.IsNullOrWhiteSpace(profile.Email) || !profile.Email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}
			var user = _userRepository.GetById(profile.Id);
			if (user == null) return false;
			string normalizedUsername = profile.Username.Trim();
			string normalizedEmail = profile.Email.Trim();
			EnsureUniqueUsername(normalizedUsername, profile.Id);
			EnsureUniqueEmail(normalizedEmail, profile.Id);
			var before = new Dictionary<string, string?>
			{
				[AuditField.Username] = user.Username,
				[AuditField.FullName] = user.FullName,
				[AuditField.Email] = user.Email,
				[AuditField.Phone] = user.PhoneNumber
			};
			user.Username = normalizedUsername;
			user.FullName = InputFormatter.FormatFullName(profile.FullName);
			user.Email = normalizedEmail;
			user.PhoneNumber = string.IsNullOrWhiteSpace(profile.PhoneNumber) ? null : profile.PhoneNumber.Trim();
			if (!_userRepository.Update(user))
			{
				return false;
			}
			var after = new Dictionary<string, string?>
			{
				[AuditField.Username] = user.Username,
				[AuditField.FullName] = user.FullName,
				[AuditField.Email] = user.Email,
				[AuditField.Phone] = user.PhoneNumber
			};
			var (oldValue, newValue) = AuditDiff.Build(before, after);
			TryAudit(new AuditLog
			{
				UserId = profile.Id,
				Action = AuditAction.UpdateProfile,
				TableName = AuditObject.Users,
				RecordId = profile.Id.ToString(),
				Detail = profile.Username,
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
			return true;
		}

		// Pass-through load user by Id (settings/profile window)
		public User? GetById(int userId)
		{
			return _userRepository.GetById(userId);
		}

		// Create Manager account: generate username/password, Role=Manager, ManagedPropertyId required
		public ManagerAccountCreatedDto CreateManagerAccount(User manager)
		{
			ArgumentNullException.ThrowIfNull(manager);
			if (string.IsNullOrWhiteSpace(manager.FullName))
			{
				throw new ArgumentException("Họ tên không được để trống.");
			}
			if (string.IsNullOrWhiteSpace(manager.Email) || !manager.Email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}
			if (!manager.ManagedPropertyId.HasValue || manager.ManagedPropertyId.Value <= 0)
			{
				throw new ArgumentException("Chọn nhà trọ để phân công.");
			}
			string normalizedEmail = manager.Email.Trim();
			EnsureUniqueEmail(normalizedEmail);
			string formattedName = InputFormatter.FormatFullName(manager.FullName);
			string username = GenerateUniqueUsername(formattedName);
			string password = IdentityGenerator.GeneratePassword();
			var newUser = new User
			{
				FullName = formattedName,
				Username = username,
				PasswordHash = HashPassword(password),
				Email = normalizedEmail,
				PhoneNumber = string.IsNullOrWhiteSpace(manager.PhoneNumber) ? null : manager.PhoneNumber.Trim(),
				IsActive = true,
				CreatedAt = DateTime.Now,
				Role = UserRole.Manager,
				ManagedPropertyId = manager.ManagedPropertyId
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

		// Reject duplicate username — excludeUserId when updating profile
		public void EnsureUniqueUsername(string username, int? excludeUserId = null)
		{
			if (_userRepository.UsernameExists(username, excludeUserId))
			{
				throw new ArgumentException("Tên đăng nhập đã được sử dụng.");
			}
		}

		// Reject duplicate email and validate basic format (@)
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

		// Retry up to 20 times to generate a unique username; truncate prefix if over 20 chars
		private string GenerateUniqueUsername(string fullName)
		{
			for (int attempt = 0; attempt < 20; attempt++)
			{
				string username = IdentityGenerator.GenerateUsername(fullName);
				if (username.Length > 20)
				{
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

		// Best-effort audit — a log write failure must not fail login/CRUD
		private void TryAudit(AuditLog log)
		{
			try
			{
				_audits.Add(log);
			}
			catch
			{
			}
		}

		// BCrypt verify plaintext against stored hash
		private bool VerifyPasswordHash(string password, string storedHash)
		{
			return BCrypt.Net.BCrypt.Verify(password, storedHash);
		}

		// BCrypt hash with default work factor
		private string HashPassword(string password)
		{
			return BCrypt.Net.BCrypt.HashPassword(password);
		}
	}
}
