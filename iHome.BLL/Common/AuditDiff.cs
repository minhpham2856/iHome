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
			// Union keys from both sides so added/removed fields are included
			var keys = before.Keys.Union(after.Keys).Distinct();
			var oldLines = new List<string>();
			var newLines = new List<string>();

			foreach (string key in keys)
			{
				before.TryGetValue(key, out string? oldRaw);
				after.TryGetValue(key, out string? newRaw);
				if (Normalize(oldRaw) == Normalize(newRaw)) continue;

				oldLines.Add($"{key}: {FormatDisplay(oldRaw)}");
				newLines.Add($"{key}: {FormatDisplay(newRaw)}");
			}

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
			return sb.Length == 0 ? stored : sb.ToString();
		}

		private static string Normalize(string? value) =>
			string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

		// Blank cells show as em dash in the audit grid
		private static string FormatDisplay(string? value) =>
			string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
	}
}

