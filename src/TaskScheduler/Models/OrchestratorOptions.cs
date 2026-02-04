namespace TaskScheduler.Models;

public class OrchestratorOptions
{
    public int NumberOfJobs { get; set; } = 10;
    public double ForcedFailureWeight { get; set; } = 0;
    public bool ForceFailure { get; set; } = false;
}