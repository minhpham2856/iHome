namespace iHome.BLL.DTOs.Manager
{
	// Single label/value pair for LiveCharts series on the manager dashboard.
	public class ChartPointDto
	{
		// Category label shown on the chart axis or legend (e.g. month, status name).
		public string Label { get; set; } = string.Empty;
		// Numeric magnitude plotted for this label (revenue, room count, etc.).
		public double Value { get; set; }
	}
}
