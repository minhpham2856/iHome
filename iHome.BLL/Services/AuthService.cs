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

		// Login: verify username/password, check IsActive, audit success or failure
		public User? Login(string username, string password)
		{
			// Look up user by username in DB
			var user = _userRepository.GetByUsername(username);
			// Not found → null (no audit to avoid username enumeration)
			if (user == null) return null;
			// Compare plaintext input to BCrypt hash stored in DB
			bool isPasswordValid = VerifyPasswordHash(password, user.PasswordHash);
			if (!isPasswordValid)
			{
				// Wrong password — audit LoginFailed with username in Detail
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
			// Disabled account — audit with AccountInactive reason
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
			// Successful login — audit with formatted NewValue
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
			// Reject null caller from UI
			ArgumentNullException.ThrowIfNull(user);
			// Record logout event for compliance trail
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
			// Reject null user reference
			ArgumentNullException.ThrowIfNull(user);
			// Reload from DB to get the latest hash
			var existing = _userRepository.GetByUsername(user.Username);
			if (existing == null) return false;
			// Verify current password before allowing change
			bool isOldPasswordValid = VerifyPasswordHash(oldPassword, existing.PasswordHash);
			if (!isOldPasswordValid) return false;
			// Enforce password complexity policy
			if (!InputValidator.IsValidPassword(newPassword))
			{
				throw new ArgumentException("Mật khẩu mới phải 8-20 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
			}
			// Hash new password with BCrypt before persistence
			existing.PasswordHash = HashPassword(newPassword);
			// Persist hash; return false if UPDATE affected zero rows
			if (!_userRepository.UpdatePassword(existing))
			{
				return false;
			}
			// Audit successful password change
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
			// Reject null lookup DTO from UI
			ArgumentNullException.ThrowIfNull(lookup);
			// Basic email format check before hitting DB
			if (string.IsNullOrWhiteSpace(lookup.Email) || !lookup.Email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}
			// Resolve active account by email address
			var user = _userRepository.GetByEmail(lookup.Email.Trim());
			if (user == null || !user.IsActive)
			{
				throw new ArgumentException("Không tìm thấy tài khoản với email này.");
			}
			// Generate one-time temporary password
			string password = IdentityGenerator.GeneratePassword();
			// Store BCrypt hash immediately (plaintext only goes to email)
			user.PasswordHash = HashPassword(password);
			if (!_userRepository.UpdatePassword(user))
			{
				throw new InvalidOperationException("Không thể đặt lại mật khẩu.");
			}
			// Audit reset request before sending email
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
			// Send plaintext temp password via SMTP (may throw)
			_emailService.SendPasswordResetInfo(user.Email, password);
		}

		// Update the signed-in user's profile — validate unique username/email, AuditDiff before/after
		public bool UpdateProfile(User profile)
		{
			// Reject null profile payload
			ArgumentNullException.ThrowIfNull(profile);
			// Validate username length and allowed characters
			if (string.IsNullOrWhiteSpace(profile.Username) || !InputValidator.IsValidUsername(profile.Username.Trim()))
			{
				throw new ArgumentException("Tên đăng nhập 3-20 ký tự, chỉ gồm chữ, số, dấu chấm hoặc gạch dưới.");
			}
			// Full name is mandatory
			if (string.IsNullOrWhiteSpace(profile.FullName))
			{
				throw new ArgumentException("Họ tên không được để trống.");
			}
			// Email must contain @
			if (string.IsNullOrWhiteSpace(profile.Email) || !profile.Email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}
			// Load current row for diff and update
			var user = _userRepository.GetById(profile.Id);
			if (user == null) return false;
			// Normalize inputs before uniqueness checks
			string normalizedUsername = profile.Username.Trim();
			string normalizedEmail = profile.Email.Trim();
			// Fail fast if username taken by another user
			EnsureUniqueUsername(normalizedUsername, profile.Id);
			// Fail fast if email taken by another user
			EnsureUniqueEmail(normalizedEmail, profile.Id);
			// Snapshot before edit for audit diff
			var before = new Dictionary<string, string?>
			{
				[AuditField.Username] = user.Username,
				[AuditField.FullName] = user.FullName,
				[AuditField.Email] = user.Email,
				[AuditField.Phone] = user.PhoneNumber
			};
			// Apply normalized profile fields to tracked entity
			user.Username = normalizedUsername;
			user.FullName = InputFormatter.FormatFullName(profile.FullName);
			user.Email = normalizedEmail;
			user.PhoneNumber = string.IsNullOrWhiteSpace(profile.PhoneNumber) ? null : profile.PhoneNumber.Trim();
			// Persist profile changes
			if (!_userRepository.Update(user))
			{
				return false;
			}
			// Build after snapshot for audit diff
			var after = new Dictionary<string, string?>
			{
				[AuditField.Username] = user.Username,
				[AuditField.FullName] = user.FullName,
				[AuditField.Email] = user.Email,
				[AuditField.Phone] = user.PhoneNumber
			};
			// Compute changed fields only
			var (oldValue, newValue) = AuditDiff.Build(before, after);
			// Write profile update audit row
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
			// Delegate to repository without extra business rules
			return _userRepository.GetById(userId);
		}

		// Create Manager account: generate username/password, Role=Manager, ManagedPropertyId required
		public ManagerAccountCreatedDto CreateManagerAccount(User manager)
		{
			// Reject null manager template from LandlordManagerService
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
			// Trim email before uniqueness check
			string normalizedEmail = manager.Email.Trim();
			EnsureUniqueEmail(normalizedEmail);
			// Title-case full name per house formatting rules
			string formattedName = InputFormatter.FormatFullName(manager.FullName);
			// Generate unique login username from full name
			string username = GenerateUniqueUsername(formattedName);
			// Generate temporary password for first login
			string password = IdentityGenerator.GeneratePassword();
			// Build new User entity with Manager role
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
			// INSERT into Users table
			if (!_userRepository.Add(newUser))
			{
				throw new InvalidOperationException("Không thể tạo tài khoản nhân viên.");
			}
			// Reload to get Id after INSERT
			var saved = _userRepository.GetByUsername(username)
				?? throw new InvalidOperationException("Không thể tải tài khoản vừa tạo.");
			// DTO returned to UI with credentials and email status
			var result = new ManagerAccountCreatedDto
			{
				UserId = saved.Id,
				Username = username,
				TemporaryPassword = password,
				EmailSent = false
			};
			// Email failure must not fail the whole flow — UI reads EmailSent/EmailError
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
			// Query repository for conflicting username
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
				// Derive candidate from full name + random suffix
				string username = IdentityGenerator.GenerateUsername(fullName);
				if (username.Length > 20)
				{
					// Keep trailing 6 random digits; truncate name prefix to fit Username column
					string suffix = username[^6..];
					string prefix = username[..^6];
					if (prefix.Length > 14) prefix = prefix[..14];
					username = prefix + suffix;
				}
				// Accept first unused username
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
				// Insert audit row; swallow any DB/SMTP side-effect errors
				_audits.Add(log);
			}
			catch
			{
				// Intentionally empty — auth flow must continue even if audit fails
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
