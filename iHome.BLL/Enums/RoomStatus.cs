using System;

namespace iHome.BLL.Enums
{
	// Room.Status values stored in DB and shown in UI (Vietnamese)
	public static class RoomStatus
	{
		public const string Occupied = "Đang ở";
		public const string Empty = "Trống";
		public const string Deposited = "Đã đặt cọc";
		public const string Maintenance = "Bảo trì";

		// True when room has no active tenants (Empty status)
		public static bool IsVacant(string status) =>
			// Case-insensitive compare against Empty constant
			string.Equals(status, Empty, StringComparison.OrdinalIgnoreCase);

		// Map arbitrary input to one of the four canonical room status strings
		public static string Normalize(string status)
		{
			// Match Occupied regardless of casing from form/combo
			if (string.Equals(status, Occupied, StringComparison.OrdinalIgnoreCase))
			{
				return Occupied;
			}
			// Match Deposited (reserved but not yet moved in)
			if (string.Equals(status, Deposited, StringComparison.OrdinalIgnoreCase))
			{
				return Deposited;
			}
			// Match Maintenance (blocked from new contracts)
			if (string.Equals(status, Maintenance, StringComparison.OrdinalIgnoreCase))
			{
				return Maintenance;
			}
			// Unknown or Empty → default to Empty (vacant)
			return Empty;
		}

		// Value is already Vietnamese display text
		public static string Format(string status) =>
			// Blank/null → show Empty label; otherwise return trimmed stored status
			string.IsNullOrWhiteSpace(status) ? Empty : status;
	}
}
