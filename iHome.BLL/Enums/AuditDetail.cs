namespace iHome.BLL.Enums
{
	// Supplemental NewValue/Detail strings for auth audit — not mapped to a specific entity field
	public static class AuditDetail
	{
		public const string WrongPassword = "Sai mật khẩu"; // LoginFailed reason
		public const string AccountInactive = "Tài khoản ngừng hoạt động";
		public const string LoginSuccess = "Đăng nhập thành công";
		public const string Logout = "Đăng xuất";
		public const string PasswordChanged = "Đã đổi mật khẩu";
		public const string TempPasswordSent = "Đã gửi mật khẩu tạm qua email";
	}
}
