namespace Common.Models;

public class PodStatusEvent
{
    public required string PodName { get; set; }
    public string? Phase { get; set; }
    public string? Message { get; set; }
    public string? Reason { get; set; }
    public string? EventType { get; set; }
    public DateTime Timestamp { get; set; }
}

