namespace iHome.BLL.Enums
{
	// AuditLog.TableName values — labels for the audited entity group
	public static class AuditObject
	{
		public const string Users = "Người dùng";
		public const string Properties = "Nhà trọ";
		public const string Buildings = "Tòa nhà";
		public const string Rooms = "Phòng";
		public const string RoomTypes = "Loại phòng";
		public const string Services = "Dịch vụ";
		public const string RoomServices = "Dịch vụ phòng";
		public const string Tenants = "Khách thuê";
		public const string Contracts = "Hợp đồng";
		public const string ContractTenants = "Khách trên hợp đồng";
		public const string Invoices = "Hóa đơn";
	}
}
