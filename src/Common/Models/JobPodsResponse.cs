namespace Common.Models;

public class JobPodsResponse
{
    public required string JobName { get; set; }
    public required string Namespace { get; set; }
    public required List<PodInfo> Pods { get; set; }
}

