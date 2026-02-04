using System.Text.Json;
using Common;
using Common.Models;
using Microsoft.Extensions.Logging;

namespace TaskScheduler;

/// <summary>
/// Orchestrates the execution of Kubernetes jobs: creates a job, waits for completion, and retrieves logs.
/// </summary>
public class KubernetesJobOrchestrator
{
    private readonly K8sControllerClient _k8sClient;
    private readonly ILogger<KubernetesJobOrchestrator> _logger;

    public KubernetesJobOrchestrator(
        K8sControllerClient k8sClient,
        ILogger<KubernetesJobOrchestrator> logger)
    {
        _k8sClient = k8sClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Kubernetes job, waits for it to complete, and returns the job logs.
    /// Uses SSE (Server-Sent Events) to watch the pod status until completion.
    /// </summary>
    /// <param name="request">Optional request with namespace override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The logs from the completed job.</returns>
    public async Task<string> ExecuteJobAndGetLogsAsync(
        CreateJobRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Kubernetes job execution");

        // Step 1: Create the job
        var createResponse = await _k8sClient.CreateJobAsync(request, cancellationToken);
        _logger.LogInformation(
            "Created Kubernetes job '{JobName}' in namespace '{Namespace}'",
            createResponse.JobName,
            createResponse.Namespace);

        // Step 2: Wait for the pod to be created and get its name
        // Pods might not be created immediately, so we wait a bit and retry
        PodInfo? podInfo = null;
        var maxRetries = 10;
        var retryCount = 0;

        while (podInfo == null && retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            
            var podsResponse = await _k8sClient.GetJobPodsAsync(
                createResponse.JobName,
                createResponse.Namespace,
                cancellationToken);

            podInfo = podsResponse.Pods.FirstOrDefault();
            
            if (podInfo == null)
            {
                retryCount++;
                _logger.LogInformation(
                    "Waiting for pod to be created for job '{JobName}' (attempt {Attempt}/{MaxAttempts})",
                    createResponse.JobName,
                    retryCount,
                    maxRetries);
            }
        }

        if (podInfo == null)
        {
            throw new InvalidOperationException(
                $"No pod found for job '{createResponse.JobName}' after {maxRetries} attempts");
        }

        _logger.LogInformation(
            "Found pod '{PodName}' for job '{JobName}'",
            podInfo.PodName,
            createResponse.JobName);

        // Step 3: Watch the pod using SSE until it reaches a terminal state
        _logger.LogInformation(
            "Watching pod '{PodName}' via SSE until completion",
            podInfo.PodName);

        var finalStatus = await _k8sClient.WaitForPodCompletionAsync(
            podInfo.PodName,
            createResponse.Namespace,
            onStatusUpdate: statusEvent =>
            {
                _logger.LogInformation(
                    "Pod '{PodName}' status update: Phase={Phase}, Reason={Reason}",
                    statusEvent.PodName,
                    statusEvent.Phase,
                    statusEvent.Reason);
            },
            cancellationToken);

        _logger.LogInformation(
            "Pod '{PodName}' completed with phase '{Phase}'",
            podInfo.PodName,
            finalStatus.Phase);

        // Check if the pod failed (exit code != 0)
        if (finalStatus.Phase == "Failed")
        {
            _logger.LogError(
                "Pod '{PodName}' for job '{JobName}' failed with phase '{Phase}'. Reason: {Reason}",
                podInfo.PodName,
                createResponse.JobName,
                finalStatus.Phase,
                finalStatus.Reason ?? "Unknown");
            
            throw new JobFailedException(
                $"Kubernetes job '{createResponse.JobName}' failed. Pod '{podInfo.PodName}' exited with failure. Reason: {finalStatus.Reason ?? "Unknown"}",
                createResponse.JobName,
                podInfo.PodName,
                finalStatus.Phase);
        }

        // Step 4: Get the logs from the completed pod
        _logger.LogInformation(
            "Retrieving logs from pod '{PodName}'",
            podInfo.PodName);

        var logsResponse = await _k8sClient.GetPodLogsAsync(
            podInfo.PodName,
            createResponse.Namespace,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Retrieved {LogLength} characters of logs from pod '{PodName}'",
            logsResponse.Logs.Length,
            podInfo.PodName);

        return logsResponse.Logs;
    }

    /// <summary>
    /// Creates a Kubernetes job, waits for it to complete, extracts JSON output from logs, and returns a structured result.
    /// Uses SSE (Server-Sent Events) to watch the pod status until completion.
    /// Throws JobFailedException if the job fails or any task has a "Failed" status.
    /// </summary>
    /// <param name="request">Optional request with namespace override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The structured job result extracted from the logs.</returns>
    /// <exception cref="JobFailedException">Thrown when the job fails or any task has "Failed" status.</exception>
    public async Task<JobResult> ExecuteJobAndGetResultAsync(
        CreateJobRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var logs = await ExecuteJobAndGetLogsAsync(request, cancellationToken);
        
        _logger.LogInformation("Extracting JSON result from logs");
        
        var result = ExtractJsonFromLogs(logs);
        
        _logger.LogInformation(
            "Extracted job result: {Summary} (TotalTasks: {TotalTasks}, Successful: {SuccessfulTasks}, Failed: {FailedTasks})",
            result.Summary,
            result.TotalTasks,
            result.SuccessfulTasks,
            result.FailedTasks);

        // Check if any task failed
        if (result.FailedTasks > 0)
        {
            var failedTaskNames = result.TaskResults
                .Where(t => t.Status == "Failed")
                .Select(t => t.TaskName)
                .ToList();
            
            _logger.LogError(
                "Job completed but {FailedCount} task(s) failed: {FailedTasks}",
                result.FailedTasks,
                string.Join(", ", failedTaskNames));
            
            throw new JobFailedException(
                $"Job completed but {result.FailedTasks} task(s) failed: {string.Join(", ", failedTaskNames)}",
                result);
        }
        
        return result;
    }

    /// <summary>
    /// Extracts JSON output from the worker logs.
    /// Looks for JSON between "=== JSON_OUTPUT_START ===" and "=== JSON_OUTPUT_END ===" markers.
    /// </summary>
    /// <param name="logs">The full log output from the worker.</param>
    /// <returns>The parsed job result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when JSON cannot be found or parsed.</exception>
    private JobResult ExtractJsonFromLogs(string logs)
    {
        const string startMarker = "=== JSON_OUTPUT_START ===";
        const string endMarker = "=== JSON_OUTPUT_END ===";

        var startIndex = logs.IndexOf(startMarker, StringComparison.Ordinal);
        if (startIndex == -1)
        {
            throw new InvalidOperationException(
                "Could not find JSON output start marker in logs. Expected '=== JSON_OUTPUT_START ==='");
        }

        var endIndex = logs.IndexOf(endMarker, startIndex, StringComparison.Ordinal);
        if (endIndex == -1)
        {
            throw new InvalidOperationException(
                "Could not find JSON output end marker in logs. Expected '=== JSON_OUTPUT_END ==='");
        }

        var jsonStart = startIndex + startMarker.Length;
        var jsonLength = endIndex - jsonStart;
        var jsonContent = logs.Substring(jsonStart, jsonLength).Trim();

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            throw new InvalidOperationException("JSON content between markers is empty");
        }

        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var result = JsonSerializer.Deserialize<JobResult>(jsonContent, jsonOptions);
            
            if (result == null)
            {
                throw new InvalidOperationException("Deserialized JSON result is null");
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse JSON from logs: {ex.Message}. JSON content: {jsonContent[..Math.Min(200, jsonContent.Length)]}...",
                ex);
        }
    }
}

