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
			string.Equals(status, Empty, StringComparison.OrdinalIgnoreCase);

		// Map arbitrary input to one of the four canonical room status strings
		public static string Normalize(string status)
		{
			if (string.Equals(status, Occupied, StringComparison.OrdinalIgnoreCase))
			{
				return Occupied;
			}
			if (string.Equals(status, Deposited, StringComparison.OrdinalIgnoreCase))
			{
				return Deposited;
			}
			if (string.Equals(status, Maintenance, StringComparison.OrdinalIgnoreCase))
			{
				return Maintenance;
			}
			return Empty;
		}

		// Value is already Vietnamese display text
		public static string Format(string status) =>
			string.IsNullOrWhiteSpace(status) ? Empty : status;
	}
}
