namespace iHome.BLL.Enums
{
	// Vietnamese labels written to AuditLog.Action — describe the user action type
	public static class AuditAction
	{
		public const string Create = "Tạo mới"; // INSERT entity
		public const string Update = "Cập nhật"; // UPDATE entity fields
		public const string Delete = "Xóa"; // Hard delete
		public const string Disable = "Ngừng hoạt động"; // Soft deactivate
		public const string AssignService = "Gán dịch vụ"; // RoomServices sync
		public const string UnassignService = "Bỏ gán dịch vụ";
		public const string AssignTenant = "Gán khách"; // ContractTenants (Manager)
		public const string RemoveTenant = "Gỡ khách";
		public const string Login = "Đăng nhập"; // Successful auth
		public const string LoginFailed = "Đăng nhập thất bại";
		public const string Logout = "Đăng xuất";
		public const string Register = "Đăng ký";
		public const string ChangePassword = "Đổi mật khẩu";
		public const string ForgotPassword = "Quên mật khẩu";
		public const string UpdateProfile = "Cập nhật hồ sơ";
	}
}
