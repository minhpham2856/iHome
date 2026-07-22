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

		private readonly IHomeDbContext _context;

		public ContractRepository() : this(new IHomeDbContext())
		{
		}

		public ContractRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// All contracts.
		public List<Contract> GetAll()
		{
			return _context.Contracts.ToList();
		}

		// Count contracts with the given status label.
		public int CountByStatus(string status)
		{
			return _context.Contracts.Count(c => c.Status == status);
		}

		// Active contracts expiring within one calendar month from today.
		public List<Contract> ExpiringSoon(string status, int days)
		{
			DateOnly today = DateOnly.FromDateTime(DateTime.Today);
			DateOnly limit = today.AddMonths(1);
			return _context.Contracts
				.Where(c => c.Status == status && c.EndDate >= today && c.EndDate < limit)
				.ToList();
		}

		// Landlord-scoped contracts, optionally filtered by property or building.
		public List<Contract> GetForLandlord(int landlordId, int? propertyId = null, int? buildingId = null)
		{
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

		// One contract by id when it belongs to the landlord.
		public Contract? GetByIdForLandlord(int landlordId, int contractId)
		{
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

		// True when the contract belongs to a room under the landlord.
		public bool IsOwned(int landlordId, int contractId)
		{
			return _context.Contracts.Any(c =>
				c.Id == contractId &&
				c.Room.Building.Property.LandlordId == landlordId);
		}

		// Update contract; check date overlap then refresh room status.
		public void Update(Contract changes)
		{
			var contract = _context.Contracts
				.Include(c => c.Room)
				.Single(c => c.Id == changes.Id);

			// When becoming active: room must not overlap another active contract's dates.
			if (changes.Status == ActiveContract)
			{
				EnsureRoomAvailable(contract.RoomId, changes.StartDate, changes.EndDate, contract.Id);
			}

			contract.StartDate = changes.StartDate;
			contract.EndDate = changes.EndDate;
			contract.MonthlyRent = changes.MonthlyRent;
			contract.DepositAmount = changes.DepositAmount;
			contract.Status = changes.Status;
			contract.Notes = changes.Notes;
			_context.SaveChanges();
			// Refresh room occupancy from active contracts covering today.
			RefreshRoomStatus(contract.RoomId);
		}

		// Date overlap: StartA <= EndB && EndA >= StartB on other active contracts for the room.
		private void EnsureRoomAvailable(
			int roomId,
			DateOnly startDate,
			DateOnly endDate,
			int excludingContractId)
		{
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

		// Refresh Empty/Occupied from active contracts covering today (skip Bảo trì / Đã đặt cọc).
		private void RefreshRoomStatus(int roomId)
		{
			var room = _context.Rooms.Single(r => r.Id == roomId);
			if (string.Equals(room.Status, "Bảo trì", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(room.Status, "Đã đặt cọc", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			DateOnly today = DateOnly.FromDateTime(DateTime.Today);
			room.Status = _context.Contracts.Any(c =>
				c.RoomId == roomId &&
				c.Status == ActiveContract &&
				c.StartDate <= today &&
				c.EndDate >= today)
				? OccupiedRoom
				: EmptyRoom;
			_context.SaveChanges();
		}
	}
}
