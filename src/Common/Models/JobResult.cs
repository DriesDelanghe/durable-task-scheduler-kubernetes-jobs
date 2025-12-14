using System.Text.Json.Serialization;

namespace Common.Models;

/// <summary>
/// Represents the structured result from a completed Kubernetes job.
/// </summary>
public class JobResult
{
    [JsonPropertyName("workerId")]
    public string WorkerId { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("totalTasks")]
    public int TotalTasks { get; set; }

    [JsonPropertyName("successfulTasks")]
    public int SuccessfulTasks { get; set; }

    [JsonPropertyName("failedTasks")]
    public int FailedTasks { get; set; }

    [JsonPropertyName("taskResults")]
    public List<TaskResult> TaskResults { get; set; } = new();

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Represents a single task result within a job.
/// </summary>
public class TaskResult
{
    [JsonPropertyName("taskName")]
    public string TaskName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("durationMs")]
    public int DurationMs { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; set; }
}

