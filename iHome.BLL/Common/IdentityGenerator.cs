using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace iHome.BLL.Common
{
	public static class IdentityGenerator
	{
		private const int PasswordLength = 12;

		// Generates a random password of length 12.
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

		// Ví dụ: Nguyễn Văn Bình → binhnv + 6 số ngẫu nhiên
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

		public static int GenerateRandomId() => Random.Shared.Next(100000, 999999);

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
