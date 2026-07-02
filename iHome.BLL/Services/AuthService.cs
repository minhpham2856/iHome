using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using iHome.DAL.Entities;
using iHome.BLL.Common;

namespace iHome.BLL.Services
{
	public class AuthService
	{
		private readonly UserRepository _userRepository = new();
		private readonly EmailService _emailService = new();

		public AuthService() { }

		// main methods
		public bool Login(string username, string password)
		{
			var user = _userRepository.GetByUsername(username);

			// check if user exists
			if (user == null) return false;

			// check if password is correct
			bool isPasswordValid = VerifyPasswordHash(password, user.PasswordHash);
			if (!isPasswordValid) return false;

			return true;
		}

		public bool Register(User user)
		{
			// check if user already exists
			var existingUser = _userRepository.GetByUsername(user.Username);
			if (existingUser != null) return false;

			// create new user
			var newUser = new User
			{
				FullName = InputFormatter.FormatFullName(user.FullName),
				Username = user.Username,
				PasswordHash = HashPassword(IdentityGenerator.GeneratePassword()),
				Email = user.Email,
				PhoneNumber = user.PhoneNumber,
				IsActive = true,
				CreatedAt = DateTime.Now,
				Role = user.Role
			};

			// save user to database
			return _userRepository.Add(newUser);
		}

		public bool ChangePassword(string username, string oldPassword, string newPassword)
		{
			// check if user exists
			var user = _userRepository.GetByUsername(username);
			if (user == null) return false;

			// check if old password is correct
			bool isOldPasswordValid = VerifyPasswordHash(oldPassword, user.PasswordHash);
			if (!isOldPasswordValid) return false;

			// update password
			user.PasswordHash = HashPassword(newPassword);
			if (_userRepository.Add(user))
			{
				return true;
			}
			return false;
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
