using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for User.
	public class UserRepository
	{
		private readonly IHomeDbContext _context;

		public UserRepository() : this(new IHomeDbContext()) { }

		public UserRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert a new user.
		public bool Add(User newUser)
		{
			_context.Users.Add(newUser);
			return _context.SaveChanges() > 0;
		}

		// One user by id.
		public User GetById(int id)
		{
			return _context.Users.FirstOrDefault(u => u.Id == id);
		}

		// First user with the given role label.
		public User GetByRole(string role)
		{
			return _context.Users.FirstOrDefault(u => u.Role == role);
		}

		// User by exact username.
		public User GetByUsername(string username)
		{
			return _context.Users.FirstOrDefault(u => u.Username == username);
		}

		// User by email (trimmed, case-insensitive); blank email returns null.
		public User? GetByEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email)) return null;
			string normalized = email.Trim().ToLower();
			return _context.Users.FirstOrDefault(u => u.Email.ToLower() == normalized);
		}

		// Active manager accounts.
		public List<User> GetManagers()
		{
			return _context.Users
				.Where(u => u.Role == "Quản lý" && u.IsActive)
				.OrderBy(u => u.FullName)
				.ToList();
		}

		// Managers tied to this landlord's properties (including inactive).
		public List<User> GetManagersForLandlord(int landlordId)
		{
			return _context.Users
				.Include(u => u.ManagedProperty)
				.Where(u =>
					u.Role == "Quản lý" &&
					u.ManagedPropertyId != null &&
					u.ManagedProperty!.LandlordId == landlordId)
				.OrderByDescending(u => u.IsActive)
				.ThenBy(u => u.FullName)
				.ToList();
		}

		// Active managers assigned to one property.
		public List<User> GetManagersForProperty(int propertyId)
		{
			return _context.Users
				.Where(u =>
					u.Role == "Quản lý" &&
					u.IsActive &&
					u.ManagedPropertyId == propertyId)
				.OrderBy(u => u.FullName)
				.ToList();
		}

		// Soft-disable an account.
		public bool Disable(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			var existing = _context.Users.FirstOrDefault(u => u.Id == user.Id);
			if (existing == null) return false;

			existing.IsActive = false;
			existing.UpdatedAt = DateTime.Now;
			return _context.SaveChanges() > 0;
		}

		// Hard-delete; call after clearing building assignments / FK checks.
		public bool Delete(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			var existing = _context.Users.FirstOrDefault(u => u.Id == user.Id);
			if (existing == null) return false;

			_context.Users.Remove(existing);
			return _context.SaveChanges() > 0;
		}

		// True when the user created any contract or received any payment.
		public bool HasContractOrPaymentReferences(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			return _context.Contracts.Any(c => c.CreatedBy == user.Id) ||
				_context.Payments.Any(p => p.ReceivedBy == user.Id);
		}

		// Remove all audit rows authored by this user.
		public void DeleteAuditLogs(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			var logs = _context.AuditLogs.Where(a => a.UserId == user.Id).ToList();
			if (logs.Count == 0) return;
			_context.AuditLogs.RemoveRange(logs);
			_context.SaveChanges();
		}

		// Update profile and assignment fields.
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

		// True when another user already owns this username.
		public bool UsernameExists(string username, int? excludeUserId = null)
		{
			string normalized = username.Trim();
			return _context.Users.Any(u =>
				u.Username == normalized &&
				(!excludeUserId.HasValue || u.Id != excludeUserId.Value));
		}

		// True when another user already owns this email (case-insensitive).
		public bool EmailExists(string email, int? excludeUserId = null)
		{
			string normalized = email.Trim().ToLower();
			return _context.Users.Any(u =>
				u.Email.ToLower() == normalized &&
				(!excludeUserId.HasValue || u.Id != excludeUserId.Value));
		}

		// Replace PasswordHash from User.Id + User.PasswordHash.
		public bool UpdatePassword(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			var existing = _context.Users.FirstOrDefault(u => u.Id == user.Id);
			if (existing == null) return false;

			existing.PasswordHash = user.PasswordHash;
			existing.UpdatedAt = DateTime.Now;
			return _context.SaveChanges() > 0;
		}
	}
}
