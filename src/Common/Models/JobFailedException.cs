namespace Common.Models;

/// <summary>
/// Exception thrown when a Kubernetes job fails.
/// </summary>
public class JobFailedException : Exception
{
    public string? JobName { get; }
    public string? PodName { get; }
    public string? Phase { get; }
    public JobResult? Result { get; }

    public JobFailedException(string message) : base(message)
    {
    }

    public JobFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public JobFailedException(string message, string? jobName, string? podName, string? phase) 
        : base(message)
    {
        JobName = jobName;
        PodName = podName;
        Phase = phase;
    }

    public JobFailedException(string message, JobResult result) 
        : base(message)
    {
        Result = result;
    }
}
