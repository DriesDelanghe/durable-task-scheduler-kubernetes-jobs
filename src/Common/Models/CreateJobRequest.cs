namespace Common.Models;

public class CreateJobRequest
{
    public string? Namespace { get; set; }
    public bool ForceFailure { get; set; } = false;
}

