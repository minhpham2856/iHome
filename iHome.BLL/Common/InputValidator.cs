using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iHome.BLL.Common
{
	public static class InputValidator
	{
		public static bool IsValidUsername(string username)
		{
			if (string.IsNullOrWhiteSpace(username)) return false;
			if (username.Length < 3 || username.Length > 20) return false;
			return username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
		}

		public static bool IsValidPassword(string password)
		{
			if (string.IsNullOrWhiteSpace(password)) return false;
			if (password.Length < 8 || password.Length > 20) return false;
			return password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit) && password.Any(c => !char.IsLetterOrDigit(c));
		}
	}
}
