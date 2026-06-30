using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using iHome.DAL.Entities;

namespace iHome.BLL.Services
{
	public class AuthService
	{
		private readonly UserRepository _userRepository = new();

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

		public bool Register(string username, string password, string role)
		{
			// check if user already exists
			var existingUser = _userRepository.GetByUsername(username);
			if (existingUser != null) return false;

			// create new user
			var newUser = new User
			{
				Username = username,
				PasswordHash = HashPassword(password),
				Role = role
			};

			// save user to database
			_userRepository.Add(newUser);
			return true;
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
			_userRepository.Add(user);
			return true;
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
