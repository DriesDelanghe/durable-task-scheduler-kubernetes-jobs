namespace Common.Models;

public class PodInfo
{
    public required string PodName { get; set; }
    public required string Namespace { get; set; }
    public string? Phase { get; set; }
    public DateTime? StartTime { get; set; }
    public string? NodeName { get; set; }
}

