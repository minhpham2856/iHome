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
			// Null, empty, or whitespace-only usernames are rejected immediately
			if (string.IsNullOrWhiteSpace(username)) return false;
			// Enforce min/max length aligned with Users.Username column and UI hints
			if (username.Length < 3 || username.Length > 20) return false;
			// Every character must be alphanumeric, underscore, or dot — no spaces or special symbols
			return username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
		}

		// Password: 8–20 chars, requires upper, lower, digit, and special character
		public static bool IsValidPassword(string password)
		{
			// Reject null/blank passwords before length checks
			if (string.IsNullOrWhiteSpace(password)) return false;
			// Enforce password length window used across register/change-password flows
			if (password.Length < 8 || password.Length > 20) return false;
			// Must contain at least one uppercase, lowercase, digit, and non-alphanumeric symbol
			return password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit) && password.Any(c => !char.IsLetterOrDigit(c));
		}
	}
}
