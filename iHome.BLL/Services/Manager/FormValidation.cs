using iHome.BLL.DTOs.Manager;
using iHome.BLL.Enums;
using System;
using System.Linq;
using System.Net.Mail;

namespace iHome.BLL.Services.Manager
{
	// Validate dùng chung BLL + UI Manager
	// Get*Error trả message (không throw) để dialog hiện MessageBox ngay trên form
	// Validate* vẫn throw ArgumentException cho tầng service
	public static class FormValidation
	{
		// Allowed contract status values on create/edit forms
		private static readonly string[] ContractStatuses =
		{
			ContractStatus.Active,
			ContractStatus.Expired,
			ContractStatus.Terminated
		};
		// Manager invoice form only supports Unpaid/Paid (not Partial in UI)
		private static readonly string[] InvoiceStatuses =
		{
			InvoiceStatus.Unpaid,
			InvoiceStatus.Paid
		};

		// Non-throwing tenant validation — returns Vietnamese error or null if OK
		public static string? GetTenantError(TenantFormDto input)
		{
			// Normalize whitespace on all string fields before rule checks
			input.FullName = input.FullName.Trim();
			input.IdCardNumber = input.IdCardNumber.Trim();
			input.PhoneNumber = input.PhoneNumber.Trim();
			input.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
			input.PermanentAddress = string.IsNullOrWhiteSpace(input.PermanentAddress)
				? null
				: input.PermanentAddress.Trim();

			// Full name length bounds per business rules
			if (input.FullName.Length < 2 || input.FullName.Length > 100)
			{
				return "Họ tên phải có từ 2 đến 100 ký tự.";
			}
			// DOB required and cannot be in the future
			if (input.DateOfBirth == default || input.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
			{
				return "Ngày sinh không hợp lệ.";
			}
			// CCCD: 9–20 digits only
			if (input.IdCardNumber.Length < 9 || input.IdCardNumber.Length > 20 ||
				!input.IdCardNumber.All(char.IsDigit))
			{
				return "CCCD phải gồm từ 9 đến 20 chữ số.";
			}
			// Phone: allow leading + then 9–15 digits
			string phoneDigits = input.PhoneNumber.TrimStart('+');
			if (phoneDigits.Length < 9 || phoneDigits.Length > 15 || !phoneDigits.All(char.IsDigit))
			{
				return "Số điện thoại không hợp lệ.";
			}
			// Optional email — validate format when provided
			if (input.Email != null)
			{
				try
				{
					// MailAddress parser throws FormatException on invalid syntax
					_ = new MailAddress(input.Email);
				}
				catch (FormatException)
				{
					return "Email không hợp lệ.";
				}
			}
			// All checks passed
			return null;
		}

		// Non-throwing contract validation
		public static string? GetContractError(ContractFormDto input, bool isCreate)
		{
			// Create requires room selection
			if (isCreate && input.RoomId <= 0)
			{
				return "Vui lòng chọn phòng.";
			}
			// Create requires main tenant
			if (isCreate && input.MainTenantId <= 0)
			{
				return "Vui lòng chọn người thuê chính.";
			}
			// End date must be strictly after start date
			if (input.StartDate == default || input.EndDate <= input.StartDate)
			{
				return "Ngày kết thúc phải sau ngày bắt đầu.";
			}
			// Quy tắc nghiệp vụ: thời hạn hợp đồng tối thiểu 1 tháng lịch
			if (input.EndDate < input.StartDate.AddMonths(1))
			{
				return "Thời hạn hợp đồng phải ít nhất 1 tháng.";
			}
			// Rent must be positive; deposit non-negative
			if (input.MonthlyRent <= 0 || input.DepositAmount < 0)
			{
				return "Tiền thuê phải lớn hơn 0 và tiền cọc không được âm.";
			}
			// Status must be one of allowed enum literals
			if (!ContractStatuses.Contains(input.Status))
			{
				return "Trạng thái hợp đồng không hợp lệ.";
			}
			// Trim notes; null when blank
			input.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
			if (input.Notes?.Length > 300)
			{
				return "Ghi chú không được vượt quá 300 ký tự.";
			}
			return null;
		}

		// Non-throwing invoice validation
		public static string? GetInvoiceError(InvoiceFormDto input, bool isCreate)
		{
			// Create requires contract link
			if (isCreate && input.ContractId <= 0)
			{
				return "Vui lòng chọn hợp đồng.";
			}
			// Due date cannot precede invoice date
			if (input.InvoiceDate == default || input.DueDate < DateOnly.FromDateTime(input.InvoiceDate))
			{
				return "Hạn thanh toán không được trước ngày lập hóa đơn.";
			}
			// Total must be positive after line calculation
			if (input.TotalAmount <= 0)
			{
				return "Tổng tiền hóa đơn phải lớn hơn 0.";
			}
			// Status limited to Unpaid/Paid on manager forms
			if (!InvoiceStatuses.Contains(input.Status))
			{
				return "Trạng thái hóa đơn không hợp lệ.";
			}
			return null;
		}

		// Throwing wrapper for service layer tenant create/update
		public static void ValidateTenant(TenantFormDto input)
		{
			string? error = GetTenantError(input);
			if (error != null)
			{
				throw new ArgumentException(error);
			}
		}

		// Throwing wrapper for service layer contract create/update
		public static void ValidateContract(ContractFormDto input, bool isCreate)
		{
			string? error = GetContractError(input, isCreate);
			if (error != null)
			{
				throw new ArgumentException(error);
			}
		}

		// Throwing wrapper for service layer invoice create/update
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
