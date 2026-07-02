using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		// Generates a username based on the user's full name (Format: Last name + Middle name + First name).
		public static string GenerateUsername(string fullName)
		{
			string[] nameParts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

			// First name
			string firstName = nameParts.Last().ToLower();

			// Initials of the remaining names
			string initials = string.Concat(
				nameParts.Take(nameParts.Length - 1)
						 .Select(p => char.ToLower(p[0]))
			);

			return $"{firstName}{initials}{GenerateRandomId()}";
		}

		// Generates a random integer ID of length 6.
		public static int GenerateRandomId() => Random.Shared.Next(100000, 999999);

	}
}
