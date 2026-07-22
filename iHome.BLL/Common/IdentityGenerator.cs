using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace iHome.BLL.Common
{
	// Generate temporary username/password when Landlord creates a Manager or ForgotPassword runs
	public static class IdentityGenerator
	{
		// Fixed length for generated temporary passwords
		private const int PasswordLength = 12;

		// Random 12-character password from upper/lower/digit/special pool
		public static string GeneratePassword()
		{
			const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
			var passwordChars = new char[PasswordLength];

			for (int i = 0; i < PasswordLength; i++)
			{
				passwordChars[i] = validChars[Random.Shared.Next(validChars.Length)];
			}

			return new string(passwordChars);
		}

		// Username from full name: given name (no diacritics) + middle/last initials + 6 digits — e.g. Nguyễn Văn Bình → binhnv123456
		public static string GenerateUsername(string fullName)
		{
			string[] nameParts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (nameParts.Length == 0)
			{
				throw new ArgumentException("Họ tên không hợp lệ để tạo tài khoản.");
			}

			string firstName = RemoveDiacritics(nameParts.Last()).ToLowerInvariant();
			string initials = string.Concat(
				nameParts.Take(nameParts.Length - 1)
					.Select(p =>
					{
						string cleaned = RemoveDiacritics(p);
						return cleaned.Length == 0 ? string.Empty : char.ToLowerInvariant(cleaned[0]).ToString();
					}));

			if (string.IsNullOrWhiteSpace(firstName))
			{
				firstName = "user";
			}

			return $"{firstName}{initials}{GenerateRandomId()}";
		}

		// Six random digits 100000–999999 — username suffix to reduce collisions
		public static int GenerateRandomId() =>
			Random.Shared.Next(100000, 999999);

		// Strip Unicode diacritics (NFD) and map đ/Đ → d/D for ASCII-safe usernames
		private static string RemoveDiacritics(string text)
		{
			string normalized = text.Normalize(NormalizationForm.FormD);
			var sb = new StringBuilder();
			foreach (char c in normalized)
			{
				UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
				if (category == UnicodeCategory.NonSpacingMark) continue;
				sb.Append(c);
			}
			return sb.ToString()
				.Normalize(NormalizationForm.FormC)
				.Replace('đ', 'd')
				.Replace('Đ', 'D');
		}
	}
}
