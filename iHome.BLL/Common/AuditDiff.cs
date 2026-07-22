using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iHome.BLL.Common
{
	// Compare before/after snapshots for audit — only records fields that actually changed (keys are Vietnamese labels)
	public static class AuditDiff
	{
		// Build OldValue/NewValue as "Field: value" per line — skip unchanged fields after normalize
		public static (string? OldValue, string? NewValue) Build(
			IReadOnlyDictionary<string, string?> before,
			IReadOnlyDictionary<string, string?> after)
		{
			// Union all keys from both dictionaries so new/removed fields are not missed
			var keys = before.Keys.Union(after.Keys).Distinct();
			var oldLines = new List<string>();
			var newLines = new List<string>();

			foreach (string key in keys)
			{
				// Read old/new values; missing key treated as null
				before.TryGetValue(key, out string? oldRaw);
				after.TryGetValue(key, out string? newRaw);
				// Trim + blank → empty string for stable comparison
				string oldNorm = Normalize(oldRaw);
				string newNorm = Normalize(newRaw);
				// Unchanged → omit from audit diff
				if (oldNorm == newNorm) continue;

				oldLines.Add($"{key}: {FormatDisplay(oldRaw)}");
				newLines.Add($"{key}: {FormatDisplay(newRaw)}");
			}

			// No lines → return null instead of empty string
			return (
				oldLines.Count == 0 ? null : string.Join("\n", oldLines),
				newLines.Count == 0 ? null : string.Join("\n", newLines));
		}

		// NewValue only (Create, Login, ForgotPassword…) — skip entries with blank values
		public static string? FormatNewOnly(IReadOnlyDictionary<string, string?> values)
		{
			var lines = values
				.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
				.Select(kv => $"{kv.Key}: {FormatDisplay(kv.Value)}")
				.ToList();
			return lines.Count == 0 ? null : string.Join("\n", lines);
		}

		// Alias of FormatNewOnly — Delete audit often needs OldValue snapshot only
		public static string? FormatOldOnly(IReadOnlyDictionary<string, string?> values) =>
			FormatNewOnly(values);

		// Normalize a stored DB string before UI display (trim each line, drop blanks)
		public static string? ToDisplay(string? stored)
		{
			if (string.IsNullOrWhiteSpace(stored)) return null;
			var sb = new StringBuilder();
			foreach (string line in stored.Split('\n'))
			{
				string trimmed = line.Trim();
				if (trimmed.Length == 0) continue;
				if (sb.Length > 0) sb.Append('\n');
				sb.Append(trimmed);
			}
			// Fallback to stored if trim left nothing (edge case)
			return sb.Length == 0 ? stored : sb.ToString();
		}

		// null/whitespace → empty; otherwise Trim for comparison
		private static string Normalize(string? value) =>
			string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

		// Display null/blank as em dash for readable audit grid cells
		private static string FormatDisplay(string? value) =>
			string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
	}
}
