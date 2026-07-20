using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class UserRepository
	{
		private readonly IHomeDbContext _context;

		public UserRepository() : this(new IHomeDbContext()) { }

		public UserRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		public bool Add(User newUser)
		{
			_context.Users.Add(newUser);
			return _context.SaveChanges() > 0;
		}

		public User GetById(int id) => _context.Users.FirstOrDefault(u => u.Id == id);
		public User GetByRole(string role) => _context.Users.FirstOrDefault(u => u.Role == role);
		public User GetByUsername(string username) => _context.Users.FirstOrDefault(u => u.Username == username);

		public User? GetByEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email)) return null;
			string normalized = email.Trim().ToLower();
			return _context.Users.FirstOrDefault(u => u.Email.ToLower() == normalized);
		}

		public List<User> GetManagers() =>
			_context.Users
				.Where(u => u.Role == "Manager" && u.IsActive)
				.OrderBy(u => u.FullName)
				.ToList();

		// nhân viên đang/đã được gán nhà trọ của landlord (kể cả ngừng, kể cả chưa có tòa)
		public List<User> GetManagersForLandlord(int landlordId) =>
			_context.Users
				.Include(u => u.ManagedProperty)
				.Where(u =>
					u.Role == "Manager" &&
					u.ManagedPropertyId != null &&
					u.ManagedProperty!.LandlordId == landlordId)
				.OrderByDescending(u => u.IsActive)
				.ThenBy(u => u.FullName)
				.ToList();

		public List<User> GetManagersForProperty(int propertyId) =>
			_context.Users
				.Where(u =>
					u.Role == "Manager" &&
					u.IsActive &&
					u.ManagedPropertyId == propertyId)
				.OrderBy(u => u.FullName)
				.ToList();

		public bool Disable(int id)
		{
			var existing = _context.Users.FirstOrDefault(u => u.Id == id);
			if (existing == null) return false;

			existing.IsActive = false;
			existing.UpdatedAt = DateTime.Now;
			return _context.SaveChanges() > 0;
		}

		// xóa hẳn tài khoản (DELETE); gọi sau khi đã bỏ gán tòa / kiểm tra FK
		public bool Delete(int id)
		{
			var existing = _context.Users.FirstOrDefault(u => u.Id == id);
			if (existing == null) return false;

			_context.Users.Remove(existing);
			return _context.SaveChanges() > 0;
		}

		public bool HasContractOrPaymentReferences(int userId) =>
			_context.Contracts.Any(c => c.CreatedBy == userId) ||
			_context.Payments.Any(p => p.ReceivedBy == userId);

		public void DeleteAuditLogs(int userId)
		{
			var logs = _context.AuditLogs.Where(a => a.UserId == userId).ToList();
			if (logs.Count == 0) return;
			_context.AuditLogs.RemoveRange(logs);
			_context.SaveChanges();
		}

		public bool Update(User user)
		{
			var existing = _context.Users.FirstOrDefault(u => u.Id == user.Id);
			if (existing == null) return false;

			existing.FullName = user.FullName;
			existing.Username = user.Username;
			existing.Email = user.Email;
			existing.PhoneNumber = user.PhoneNumber;
			existing.IsActive = user.IsActive;
			existing.ManagedPropertyId = user.ManagedPropertyId;
			existing.UpdatedAt = DateTime.Now;
			return _context.SaveChanges() > 0;
		}

		public bool UsernameExists(string username, int? excludeUserId = null)
		{
			string normalized = username.Trim();
			return _context.Users.Any(u =>
				u.Username == normalized &&
				(!excludeUserId.HasValue || u.Id != excludeUserId.Value));
		}

		public bool EmailExists(string email, int? excludeUserId = null)
		{
			string normalized = email.Trim().ToLower();
			return _context.Users.Any(u =>
				u.Email.ToLower() == normalized &&
				(!excludeUserId.HasValue || u.Id != excludeUserId.Value));
		}

		public bool UpdatePassword(int userId, string passwordHash)
		{
			var existing = _context.Users.FirstOrDefault(u => u.Id == userId);
			if (existing == null) return false;

			existing.PasswordHash = passwordHash;
			existing.UpdatedAt = DateTime.Now;
			return _context.SaveChanges() > 0;
		}
	}
}
