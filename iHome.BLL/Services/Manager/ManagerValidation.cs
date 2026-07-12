using iHome.BLL.DTOs;
using System;
using System.Linq;
using System.Net.Mail;

namespace iHome.BLL.Services.Manager
{
	internal static class ManagerValidation
	{
		private static readonly string[] ContractStatuses = { "Active", "Expired", "Terminated" };
		private static readonly string[] InvoiceStatuses = { "Unpaid", "Paid" };

		public static void ValidateTenant(ManagerTenantFormDto input)
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
				throw new ArgumentException("Họ tên phải có từ 2 đến 100 ký tự.");
			}
			if (input.DateOfBirth == default || input.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
			{
				throw new ArgumentException("Ngày sinh không hợp lệ.");
			}
			if (input.IdCardNumber.Length < 9 || input.IdCardNumber.Length > 20 ||
				!input.IdCardNumber.All(char.IsDigit))
			{
				throw new ArgumentException("CCCD phải gồm từ 9 đến 20 chữ số.");
			}
			string phoneDigits = input.PhoneNumber.TrimStart('+');
			if (phoneDigits.Length < 9 || phoneDigits.Length > 15 || !phoneDigits.All(char.IsDigit))
			{
				throw new ArgumentException("Số điện thoại không hợp lệ.");
			}
			if (input.Email != null)
			{
				try
				{
					_ = new MailAddress(input.Email);
				}
				catch (FormatException)
				{
					throw new ArgumentException("Email không hợp lệ.");
				}
			}
		}

		public static void ValidateContract(ManagerContractFormDto input, bool isCreate)
		{
			if (isCreate && input.RoomId <= 0)
			{
				throw new ArgumentException("Vui lòng chọn phòng.");
			}
			if (isCreate && input.MainTenantId <= 0)
			{
				throw new ArgumentException("Vui lòng chọn người thuê chính.");
			}
			if (input.StartDate == default || input.EndDate <= input.StartDate)
			{
				throw new ArgumentException("Ngày kết thúc phải sau ngày bắt đầu.");
			}
			if (input.MonthlyRent <= 0 || input.DepositAmount < 0)
			{
				throw new ArgumentException("Tiền thuê phải lớn hơn 0 và tiền cọc không được âm.");
			}
			if (!ContractStatuses.Contains(input.Status))
			{
				throw new ArgumentException("Trạng thái hợp đồng không hợp lệ.");
			}
			input.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
			if (input.Notes?.Length > 300)
			{
				throw new ArgumentException("Ghi chú không được vượt quá 300 ký tự.");
			}
		}

		public static void ValidateInvoice(ManagerInvoiceFormDto input, bool isCreate)
		{
			if (isCreate && input.ContractId <= 0)
			{
				throw new ArgumentException("Vui lòng chọn hợp đồng.");
			}
			if (input.InvoiceDate == default || input.DueDate < DateOnly.FromDateTime(input.InvoiceDate))
			{
				throw new ArgumentException("Hạn thanh toán không được trước ngày lập hóa đơn.");
			}
			if (input.TotalAmount <= 0)
			{
				throw new ArgumentException("Tổng tiền hóa đơn phải lớn hơn 0.");
			}
			if (!InvoiceStatuses.Contains(input.Status))
			{
				throw new ArgumentException("Trạng thái hóa đơn không hợp lệ.");
			}
		}
	}
}
