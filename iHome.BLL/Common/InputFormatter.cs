using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iHome.BLL.Common
{
	public static class InputFormatter
	{
		// Format full name to uppercase and trim whitespace
		public static string FormatFullName(string fullname)
		{
			if (string.IsNullOrWhiteSpace(fullname)) return string.Empty;
			return fullname.Trim().ToUpper();
		}
	}
}
