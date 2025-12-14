namespace Common.Models;

public class CreateJobResponse
{
    public required string JobName { get; set; }
    public required string Namespace { get; set; }
    public string? JobUid { get; set; }
    public required string LabelSelector { get; set; }
    public DateTime? CreatedAt { get; set; }
}

