using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Contract.
	public class ContractRepository
	{
		private const string ActiveContract = "Đang hoạt động";
		private const string OccupiedRoom = "Đang ở";
		private const string EmptyRoom = "Trống";

		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		public ContractRepository() : this(new IHomeDbContext())
		{
		}

		public ContractRepository(IHomeDbContext context)
		{
			// Reject a null context so every repository method has a valid DbContext to query against
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Query All records.
		public List<Contract> GetAll()
		{
			// Load every contract row without filters, includes, or ordering
			return _context.Contracts.ToList();
		}

		// CountByStatus — public entry point.
		public int CountByStatus(string status)
		{
			// Count contracts whose Status column equals the supplied status label
			return _context.Contracts.Count(c => c.Status == status);
		}

		// Active contracts expiring within one calendar month (by calendar month)
		public List<Contract> ExpiringSoon(string status, int days)
		{
			// Capture today's calendar date as the lower bound for expiry filtering
			DateOnly today = DateOnly.FromDateTime(DateTime.Today);
			// Compute the exclusive upper bound one calendar month ahead of today
			DateOnly limit = today.AddMonths(1);
			// Return contracts matching status whose EndDate falls in [today, limit)
			return _context.Contracts
				.Where(c => c.Status == status && c.EndDate >= today && c.EndDate < limit)
				.ToList();
		}

		// Query ForLandlord records.
		public List<Contract> GetForLandlord(int landlordId, int? propertyId = null, int? buildingId = null)
		{
			// Load landlord-scoped contracts with room/building/property and tenant links, optionally filtered by property or building
			return _context.Contracts
				.AsNoTracking()
				.Include(c => c.Room)
					.ThenInclude(r => r.Building)
						.ThenInclude(b => b.Property)
				.Include(c => c.ContractTenants)
					.ThenInclude(ct => ct.Tenant)
				.Where(c =>
					c.Room.Building.Property.LandlordId == landlordId &&
					(!propertyId.HasValue || c.Room.Building.PropertyId == propertyId.Value) &&
					(!buildingId.HasValue || c.Room.BuildingId == buildingId.Value))
				.OrderByDescending(c => c.StartDate)
				.ThenBy(c => c.Room.Building.Name)
				.ThenBy(c => c.Room.RoomNumber)
				.ToList();
		}

		public Contract? GetByIdForLandlord(int landlordId, int contractId)
		{
			// Load one contract by id only when it belongs to a room under the given landlord
			return _context.Contracts
				.Include(c => c.Room)
					.ThenInclude(r => r.Building)
						.ThenInclude(b => b.Property)
				.Include(c => c.ContractTenants)
					.ThenInclude(ct => ct.Tenant)
				.FirstOrDefault(c =>
					c.Id == contractId &&
					c.Room.Building.Property.LandlordId == landlordId);
		}

		// IsOwned — public entry point.
		public bool IsOwned(int landlordId, int contractId)
		{
			// Return true when a contract with this id exists under a room owned by the landlord
			return _context.Contracts.Any(c =>
				c.Id == contractId &&
				c.Room.Building.Property.LandlordId == landlordId);
		}

		// Persist changes to an existing record.
		public void Update(Contract changes)
		{
			// Load the tracked contract with its room for availability checks and status refresh
			var contract = _context.Contracts
				.Include(c => c.Room)
				.Single(c => c.Id == changes.Id);

			// When the updated contract will be active, ensure the room has no overlapping active contract
			if (changes.Status == ActiveContract)
			{
				EnsureRoomAvailable(contract.RoomId, changes.StartDate, changes.EndDate, contract.Id);
			}

			// Map editable contract fields from the incoming entity onto the tracked row
			contract.StartDate = changes.StartDate;
			contract.EndDate = changes.EndDate;
			contract.MonthlyRent = changes.MonthlyRent;
			contract.DepositAmount = changes.DepositAmount;
			contract.Status = changes.Status;
			contract.Notes = changes.Notes;
			// Commit contract field updates to the database
			_context.SaveChanges();
			// Recompute the room occupancy status based on active contracts as of today
			RefreshRoomStatus(contract.RoomId);
		}

		private void EnsureRoomAvailable(
			int roomId,
			DateOnly startDate,
			DateOnly endDate,
			int excludingContractId)
		{
			// Detect any other active contract on the same room whose date range overlaps the proposed range
			if (_context.Contracts.Any(c =>
				c.RoomId == roomId &&
				c.Status == ActiveContract &&
				c.Id != excludingContractId &&
				c.StartDate <= endDate &&
				c.EndDate >= startDate))
			{
				throw new InvalidOperationException("Phòng đã có hợp đồng hoạt động trong khoảng thời gian này.");
			}
		}

		private void RefreshRoomStatus(int roomId)
		{
			// Load the room row that must receive an updated occupancy status
			var room = _context.Rooms.Single(r => r.Id == roomId);
			// Do not overwrite manual operational statuses such as maintenance or deposit-hold
			if (string.Equals(room.Status, "Bảo trì", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(room.Status, "Đã đặt cọc", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			// Determine whether an active contract covers today's calendar date for this room
			DateOnly today = DateOnly.FromDateTime(DateTime.Today);
			room.Status = _context.Contracts.Any(c =>
				c.RoomId == roomId &&
				c.Status == ActiveContract &&
				c.StartDate <= today &&
				c.EndDate >= today)
				? OccupiedRoom
				: EmptyRoom;
			// Persist the derived room status change
			_context.SaveChanges();
		}
	}
}
