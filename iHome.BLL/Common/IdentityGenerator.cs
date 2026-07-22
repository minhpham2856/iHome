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
			// Character pool covering policy requirements (mixed case, digits, symbols)
			const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
			// Pre-allocate buffer — filled index by index below
			var passwordChars = new char[PasswordLength];

			// Pick random character from pool for each position
			for (int i = 0; i < PasswordLength; i++)
			{
				passwordChars[i] = validChars[Random.Shared.Next(validChars.Length)];
			}

			// Materialize char array as immutable string for BCrypt hashing / email body
			return new string(passwordChars);
		}

		// Username from full name: given name (no diacritics) + middle/last initials + 6 digits — e.g. Nguyễn Văn Bình → binhnv123456
		public static string GenerateUsername(string fullName)
		{
			// Split on whitespace — Vietnamese names typically family + middle + given
			string[] nameParts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			// Cannot derive username from empty name
			if (nameParts.Length == 0)
			{
				throw new ArgumentException("Họ tên không hợp lệ để tạo tài khoản.");
			}

			// Given name = last token, strip Vietnamese diacritics
			string firstName = RemoveDiacritics(nameParts.Last()).ToLowerInvariant();
			// Initials of tokens before the given name (family + middle)
			string initials = string.Concat(
				nameParts.Take(nameParts.Length - 1)
					.Select(p =>
					{
						// Strip diacritics from each prefix token before taking first letter
						string cleaned = RemoveDiacritics(p);
						// Skip empty tokens after diacritic removal
						return cleaned.Length == 0 ? string.Empty : char.ToLowerInvariant(cleaned[0]).ToString();
					}));

			// Fallback if given name stripped to nothing
			if (string.IsNullOrWhiteSpace(firstName))
			{
				firstName = "user";
			}

			// Concatenate given name + initials + random 6-digit suffix
			return $"{firstName}{initials}{GenerateRandomId()}";
		}

		// Six random digits 100000–999999 — username suffix to reduce collisions
		public static int GenerateRandomId() =>
			// Upper bound exclusive in Random.Next — 999999 yields 100000..999998; acceptable for suffix
			Random.Shared.Next(100000, 999999);

		// Strip Unicode diacritics (NFD) and map đ/Đ → d/D for ASCII-safe usernames
		private static string RemoveDiacritics(string text)
		{
			// Decompose composed characters into base + combining marks
			string normalized = text.Normalize(NormalizationForm.FormD);
			var sb = new StringBuilder();
			foreach (char c in normalized)
			{
				UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
				// Skip combining marks (tone marks)
				if (category == UnicodeCategory.NonSpacingMark) continue;
				sb.Append(c);
			}
			// Recompose + map Vietnamese đ/Đ to ASCII d/D for username compatibility
			return sb.ToString()
				.Normalize(NormalizationForm.FormC)
				.Replace('đ', 'd')
				.Replace('Đ', 'D');
		}
	}
}
