using System.Text.Json;
using System.Linq;

// Simple worker that performs some tasks and outputs JSON results
var random = new Random();
var startTime = DateTime.UtcNow;

Console.WriteLine("Starting ExampleWorker...");
Console.WriteLine($"Worker started at: {startTime:O}");

var forceFailure = Environment.GetEnvironmentVariable("FORCE_FAILURE") == "true";

// Randomize number of tasks (between 3 and 10)
var numberOfTasks = random.Next(3, 11);
var tasks = Enumerable.Range(1, numberOfTasks)
    .Select(i => $"Task{i}")
    .ToList();

var results = new List<TaskResult>();

Console.WriteLine($"Processing {tasks.Count} tasks...");

for (int i = 0; i < tasks.Count; i++)
{
    var task = tasks[i];
    Console.WriteLine($"[{DateTime.UtcNow:O}] Processing {task}...");
    
    // Randomize task duration (between 500ms and 3000ms)
    var durationMs = random.Next(500, 3001);
    var taskStartTime = DateTime.UtcNow;
    
    // Simulate work
    await Task.Delay(TimeSpan.FromMilliseconds(durationMs));
    
    // Randomize success/failure (90% success rate)
    var isSuccess = forceFailure ? false : random.Next(0, 100) < 90;
    var status = isSuccess ? "Completed" : "Failed";
    var message = isSuccess ? "completed successfully" : "failed with error";
    
    var taskResult = new TaskResult
    {
        TaskName = task,
        Status = status,
        DurationMs = durationMs,
        CompletedAt = DateTime.UtcNow
    };
    
    results.Add(taskResult);
    Console.WriteLine($"[{DateTime.UtcNow:O}] {task} {message} (duration: {durationMs}ms)");
}

var endTime = DateTime.UtcNow;
var successfulTasks = results.Count(r => r.Status == "Completed");
var failedTasks = results.Count(r => r.Status == "Failed");

Console.WriteLine($"All tasks completed. Total: {results.Count} (Successful: {successfulTasks}, Failed: {failedTasks})");

// Create final result
var finalResult = new WorkerResult
{
    WorkerId = Environment.GetEnvironmentVariable("HOSTNAME") ?? $"worker-{random.Next(1000, 9999)}",
    StartTime = startTime,
    EndTime = endTime,
    TotalTasks = results.Count,
    SuccessfulTasks = successfulTasks,
    FailedTasks = failedTasks,
    TaskResults = results,
    Summary = $"Processed {results.Count} tasks: {successfulTasks} successful, {failedTasks} failed"
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

if (forceFailure)
{
    Console.WriteLine("Force failure is enabled. Exiting with error code 1.");
    Environment.Exit(1);
}
else if (failedTasks > 0)
{
    Console.WriteLine("Failed tasks are present. Exiting with error code 1.");
    Environment.Exit(1);
}
else
{
    Console.WriteLine("All tasks completed successfully. Exiting with error code 0.");
    Environment.Exit(0);
}

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
