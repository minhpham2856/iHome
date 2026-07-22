using System;
using System.Linq;

namespace iHome.BLL.Common
{
	// Static username/password validator — used by AuthService before DB writes
	public static class InputValidator
	{
		// Username: 3–20 chars, letters/digits/underscore/dot only (matches UI registration/profile rules)
		public static bool IsValidUsername(string username)
		{
			if (string.IsNullOrWhiteSpace(username)) return false;
			if (username.Length < 3 || username.Length > 20) return false;
			return username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
		}

		// Password: 8–20 chars, requires upper, lower, digit, and special character
		public static bool IsValidPassword(string password)
		{
			if (string.IsNullOrWhiteSpace(password)) return false;
			if (password.Length < 8 || password.Length > 20) return false;
			return password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit) && password.Any(c => !char.IsLetterOrDigit(c));
		}
	}
}
