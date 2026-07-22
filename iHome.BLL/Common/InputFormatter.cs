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
			// Blank input → empty string (caller validates required fields separately)
			if (string.IsNullOrWhiteSpace(fullname))
				return string.Empty;

			// Culture-aware text helper for ToTitleCase (respects current UI culture)
			TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

			// Trim edges, collapse whitespace, lowercase each token then title-case per word
			return string.Join(" ",
				fullname.Trim()
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(word => textInfo.ToTitleCase(word.ToLower())));
		}
	}
}
