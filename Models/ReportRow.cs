namespace ServiceDeskPro.Models;

// A simple reusable row for analytics and reports.
// The Bar property is a small text-based visual bar that looks clean in WPF tables.
public class ReportRow
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Bar { get; set; } = string.Empty;
}
