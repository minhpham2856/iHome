using iHome.BLL.DTOs.Manager;
using iHome.BLL.Enums;
using System;
using System.Linq;
using System.Net.Mail;

namespace iHome.BLL.Services.Manager
{
	// Validate* vẫn throw ArgumentException cho tầng service
	public static class FormValidation
	{
		// Trạng thái HĐ hợp lệ trên form tạo/sửa
		private static readonly string[] ContractStatuses =
		{
			ContractStatus.Active,
			ContractStatus.Expired,
			ContractStatus.Terminated
		};
		// Form hóa đơn Manager chỉ Unpaid/Paid (không Partial trên UI)
		private static readonly string[] InvoiceStatuses =
		{
			InvoiceStatus.Unpaid,
			InvoiceStatus.Paid
		};

		// Validate khách không throw — trả lỗi tiếng Việt hoặc null
		public static string? GetTenantError(TenantFormDto input)
		{
			input.FullName = input.FullName.Trim();
			input.IdCardNumber = input.IdCardNumber.Trim();
			input.PhoneNumber = input.PhoneNumber.Trim();
			input.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
			input.PermanentAddress = string.IsNullOrWhiteSpace(input.PermanentAddress)
				? null
				: input.PermanentAddress.Trim();

			if (input.FullName.Length < 2 || input.FullName.Length > 100)
			{
				return "Họ tên phải có từ 2 đến 100 ký tự.";
			}
			if (input.DateOfBirth == default || input.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
			{
				return "Ngày sinh không hợp lệ.";
			}
			if (input.IdCardNumber.Length < 9 || input.IdCardNumber.Length > 20 ||
				!input.IdCardNumber.All(char.IsDigit))
			{
				return "CCCD phải gồm từ 9 đến 20 chữ số.";
			}
			string phoneDigits = input.PhoneNumber.TrimStart('+');
			if (phoneDigits.Length < 9 || phoneDigits.Length > 15 || !phoneDigits.All(char.IsDigit))
			{
				return "Số điện thoại không hợp lệ.";
			}
			if (input.Email != null)
			{
				try
				{
					_ = new MailAddress(input.Email);
				}
				catch (FormatException)
				{
					return "Email không hợp lệ.";
				}
			}
			return null;
		}

		// Validate HĐ không throw
		public static string? GetContractError(ContractFormDto input, bool isCreate)
		{
			if (isCreate && input.RoomId <= 0)
			{
				return "Vui lòng chọn phòng.";
			}
			if (isCreate && input.MainTenantId <= 0)
			{
				return "Vui lòng chọn người thuê chính.";
			}
			if (input.StartDate == default || input.EndDate <= input.StartDate)
			{
				return "Ngày kết thúc phải sau ngày bắt đầu.";
			}
			if (input.EndDate < input.StartDate.AddMonths(1))
			{
				return "Thời hạn hợp đồng phải ít nhất 1 tháng.";
			}
			if (input.MonthlyRent <= 0 || input.DepositAmount < 0)
			{
				return "Tiền thuê phải lớn hơn 0 và tiền cọc không được âm.";
			}
			if (!ContractStatuses.Contains(input.Status))
			{
				return "Trạng thái hợp đồng không hợp lệ.";
			}
			input.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
			if (input.Notes?.Length > 300)
			{
				return "Ghi chú không được vượt quá 300 ký tự.";
			}
			return null;
		}

		// Validate hóa đơn không throw
		public static string? GetInvoiceError(InvoiceFormDto input, bool isCreate)
		{
			if (isCreate && input.ContractId <= 0)
			{
				return "Vui lòng chọn hợp đồng.";
			}
			if (input.InvoiceDate == default || input.DueDate < DateOnly.FromDateTime(input.InvoiceDate))
			{
				return "Hạn thanh toán không được trước ngày lập hóa đơn.";
			}
			if (input.TotalAmount <= 0)
			{
				return "Tổng tiền hóa đơn phải lớn hơn 0.";
			}
			if (!InvoiceStatuses.Contains(input.Status))
			{
				return "Trạng thái hóa đơn không hợp lệ.";
			}
			return null;
		}

		// Wrapper throw cho service tạo/sửa khách
		public static void ValidateTenant(TenantFormDto input)
		{
			string? error = GetTenantError(input);
			if (error != null)
			{
				throw new ArgumentException(error);
			}
		}

		// Wrapper throw cho service tạo/sửa HĐ
		public static void ValidateContract(ContractFormDto input, bool isCreate)
		{
			string? error = GetContractError(input, isCreate);
			if (error != null)
			{
				throw new ArgumentException(error);
			}
		}

		// Wrapper throw cho service tạo/sửa hóa đơn
		public static void ValidateInvoice(InvoiceFormDto input, bool isCreate)
		{
			string? error = GetInvoiceError(input, isCreate);
			if (error != null)
			{
				throw new ArgumentException(error);
			}
		}
	}
}
