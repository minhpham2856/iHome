using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iHome.BLL.Common
{
	public static class InputFormatter
	{
		// Format full name
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
