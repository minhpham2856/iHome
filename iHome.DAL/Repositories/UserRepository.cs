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
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		public UserRepository() : this(new IHomeDbContext()) { }

		public UserRepository(IHomeDbContext context)
		{
			// Reject a null context so every repository method has a valid DbContext to query against
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert and save.
		public bool Add(User newUser)
		{
			// Stage the new user entity for insert in the change tracker
			_context.Users.Add(newUser);
			// Persist the insert and return whether at least one row was written
			return _context.SaveChanges() > 0;
		}

		// Load one record by id.
		public User GetById(int id)
		{
			// Load one user row by primary key without additional includes
			return _context.Users.FirstOrDefault(u => u.Id == id);
		}

		// Query ByRole records.
		public User GetByRole(string role)
		{
			// Return the first user whose Role column matches the supplied role label
			return _context.Users.FirstOrDefault(u => u.Role == role);
		}

		// Query ByUsername records.
		public User GetByUsername(string username)
		{
			// Return the user row whose Username column matches exactly
			return _context.Users.FirstOrDefault(u => u.Username == username);
		}

		public User? GetByEmail(string email)
		{
			// Treat blank email as a non-query and return null without hitting the database
			if (string.IsNullOrWhiteSpace(email)) return null;
			// Normalize email to trimmed lowercase for case-insensitive lookup
			string normalized = email.Trim().ToLower();
			// Return the first user whose Email matches the normalized value
			return _context.Users.FirstOrDefault(u => u.Email.ToLower() == normalized);
		}

		// Query Managers records.
		public List<User> GetManagers()
		{
			// Load active manager accounts ordered alphabetically by full name
			return _context.Users
				.Where(u => u.Role == "Quản lý" && u.IsActive)
				.OrderBy(u => u.FullName)
				.ToList();
		}

		// Managers assigned to this landlord's properties (including inactive, even without buildings)
		public List<User> GetManagersForLandlord(int landlordId)
		{
			// Load managers tied to properties owned by the landlord, including inactive accounts
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

		// Query ManagersForProperty records.
		public List<User> GetManagersForProperty(int propertyId)
		{
			// Load active managers explicitly assigned to one property id
			return _context.Users
				.Where(u =>
					u.Role == "Quản lý" &&
					u.IsActive &&
					u.ManagedPropertyId == propertyId)
				.OrderBy(u => u.FullName)
				.ToList();
		}

		// Disable when allowed by rules.
		public bool Disable(User user)
		{
			// Guard against null user input before lookup
			ArgumentNullException.ThrowIfNull(user);
			// Reload the user row from the database by id
			var existing = _context.Users.FirstOrDefault(u => u.Id == user.Id);
			// Return false when the user was already deleted or never existed
			if (existing == null) return false;

			// Soft-disable the account and stamp the update time
			existing.IsActive = false;
			existing.UpdatedAt = DateTime.Now;
			// Persist the deactivation and report whether a row was updated
			return _context.SaveChanges() > 0;
		}

		// Hard-delete account (DELETE); call after clearing building assignments / FK checks
		public bool Delete(User user)
		{
			// Guard against null user input before lookup
			ArgumentNullException.ThrowIfNull(user);
			// Reload the user row from the database by id
			var existing = _context.Users.FirstOrDefault(u => u.Id == user.Id);
			// Return false when the user was already deleted or never existed
			if (existing == null) return false;

			// Remove the user row from the change tracker for hard delete
			_context.Users.Remove(existing);
			// Persist the delete and report whether a row was removed
			return _context.SaveChanges() > 0;
		}

		// HasContractOrPaymentReferences — public entry point.
		public bool HasContractOrPaymentReferences(User user)
		{
			// Guard against null user input before reference checks
			ArgumentNullException.ThrowIfNull(user);
			// Return true when the user created any contract or received any payment
			return _context.Contracts.Any(c => c.CreatedBy == user.Id) ||
				_context.Payments.Any(p => p.ReceivedBy == user.Id);
		}

		// DeleteAuditLogs when allowed by rules.
		public void DeleteAuditLogs(User user)
		{
			// Guard against null user input before querying audit rows
			ArgumentNullException.ThrowIfNull(user);
			// Load every audit log row authored by this user into memory
			var logs = _context.AuditLogs.Where(a => a.UserId == user.Id).ToList();
			// Exit early when there is nothing to delete
			if (logs.Count == 0) return;
			// Stage all matching audit rows for bulk removal
			_context.AuditLogs.RemoveRange(logs);
			// Commit deletion of the user's audit history
			_context.SaveChanges();
		}

		// Persist changes to an existing record.
		public bool Update(User user)
		{
			// Load the tracked user row matching the incoming entity id
			var existing = _context.Users.FirstOrDefault(u => u.Id == user.Id);
			// Return false when the target row no longer exists
			if (existing == null) return false;

			// Map editable profile and assignment fields from the incoming entity onto the tracked row
			existing.FullName = user.FullName;
			existing.Username = user.Username;
			existing.Email = user.Email;
			existing.PhoneNumber = user.PhoneNumber;
			existing.IsActive = user.IsActive;
			existing.ManagedPropertyId = user.ManagedPropertyId;
			existing.UpdatedAt = DateTime.Now;
			// Commit user updates and report whether a row was written
			return _context.SaveChanges() > 0;
		}

		// UsernameExists — public entry point.
		public bool UsernameExists(string username, int? excludeUserId = null)
		{
			// Normalize username by trimming whitespace before uniqueness comparison
			string normalized = username.Trim();
			// Return true when another user already owns this username
			return _context.Users.Any(u =>
				u.Username == normalized &&
				(!excludeUserId.HasValue || u.Id != excludeUserId.Value));
		}

		// EmailExists — public entry point.
		public bool EmailExists(string email, int? excludeUserId = null)
		{
			// Normalize email to trimmed lowercase for case-insensitive uniqueness comparison
			string normalized = email.Trim().ToLower();
			// Return true when another user already owns this email address
			return _context.Users.Any(u =>
				u.Email.ToLower() == normalized &&
				(!excludeUserId.HasValue || u.Id != excludeUserId.Value));
		}

		// Update password from User.Id + User.PasswordHash
		public bool UpdatePassword(User user)
		{
			// Guard against null user input before password update
			ArgumentNullException.ThrowIfNull(user);
			// Load the tracked user row matching the incoming entity id
			var existing = _context.Users.FirstOrDefault(u => u.Id == user.Id);
			// Return false when the target row no longer exists
			if (existing == null) return false;

			// Replace the stored password hash and stamp the update time
			existing.PasswordHash = user.PasswordHash;
			existing.UpdatedAt = DateTime.Now;
			// Commit the password change and report whether a row was written
			return _context.SaveChanges() > 0;
		}
	}
}
