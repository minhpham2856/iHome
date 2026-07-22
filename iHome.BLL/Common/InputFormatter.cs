using System;
using System.Globalization;
using System.Linq;

namespace iHome.BLL.Common
{
	// Normalize form input strings before saving to entity/DTO
	public static class InputFormatter
	{
		// Title-case each word of a full name using current culture
		public static string FormatFullName(string fullname)
		{
			if (string.IsNullOrWhiteSpace(fullname))
				return string.Empty;

			TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

			return string.Join(" ",
				fullname.Trim()
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(word => textInfo.ToTitleCase(word.ToLower())));
		}
	}
}
