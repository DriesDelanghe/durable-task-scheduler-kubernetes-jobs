using System.Text.Json;

// Simple worker that performs some tasks and outputs JSON results
Console.WriteLine("Starting ExampleWorker...");
Console.WriteLine($"Worker started at: {DateTime.UtcNow:O}");

// Simulate some work
var tasks = new List<string> { "Task1", "Task2", "Task3", "Task4", "Task5" };
var results = new List<TaskResult>();

Console.WriteLine($"Processing {tasks.Count} tasks...");

for (int i = 0; i < tasks.Count; i++)
{
    var task = tasks[i];
    Console.WriteLine($"[{DateTime.UtcNow:O}] Processing {task}...");
    
    // Simulate work
    await Task.Delay(TimeSpan.FromSeconds(1));
    
    var taskResult = new TaskResult
    {
        TaskName = task,
        Status = "Completed",
        DurationMs = 1000,
        CompletedAt = DateTime.UtcNow
    };
    
    results.Add(taskResult);
    Console.WriteLine($"[{DateTime.UtcNow:O}] {task} completed successfully");
}

Console.WriteLine($"All tasks completed. Total: {results.Count}");

// Create final result
var finalResult = new WorkerResult
{
    WorkerId = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown",
    StartTime = DateTime.UtcNow.AddSeconds(-tasks.Count),
    EndTime = DateTime.UtcNow,
    TotalTasks = results.Count,
    SuccessfulTasks = results.Count,
    FailedTasks = 0,
    TaskResults = results,
    Summary = $"Successfully processed {results.Count} tasks"
};

// Output completion indicator
Console.WriteLine("=== WORKER_COMPLETED ===");

// Output JSON result
var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

var jsonOutput = JsonSerializer.Serialize(finalResult, jsonOptions);
Console.WriteLine("=== JSON_OUTPUT_START ===");
Console.WriteLine(jsonOutput);
Console.WriteLine("=== JSON_OUTPUT_END ===");

Console.WriteLine("ExampleWorker finished.");

// Models
public class TaskResult
{
    public string TaskName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class WorkerResult
{
    public string WorkerId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalTasks { get; set; }
    public int SuccessfulTasks { get; set; }
    public int FailedTasks { get; set; }
    public List<TaskResult> TaskResults { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}
