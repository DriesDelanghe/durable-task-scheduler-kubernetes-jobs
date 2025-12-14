namespace Common.Models;

public class PodLogsResponse
{
    public required string PodName { get; set; }
    public required string Namespace { get; set; }
    public string? Container { get; set; }
    public required string Logs { get; set; }
    public DateTime RetrievedAt { get; set; }
}

