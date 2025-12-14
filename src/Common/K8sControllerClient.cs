using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Common.Models;

namespace Common;

public class K8sControllerClient
{
    private readonly HttpClient _httpClient;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public K8sControllerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Creates a new Kubernetes job running the ExampleWorker.
    /// </summary>
    /// <param name="request">Optional request with namespace override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the created job.</returns>
    public async Task<CreateJobResponse> CreateJobAsync(
        CreateJobRequest? request = null, 
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/jobs", request ?? new CreateJobRequest(), JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<CreateJobResponse>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    /// <summary>
    /// Gets all pods created by a specific job.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="ns">Optional namespace override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the job's pods.</returns>
    public async Task<JobPodsResponse> GetJobPodsAsync(
        string jobName, 
        string? ns = null, 
        CancellationToken cancellationToken = default)
    {
        var url = $"/jobs/{Uri.EscapeDataString(jobName)}/pods";
        if (!string.IsNullOrEmpty(ns))
        {
            url += $"?ns={Uri.EscapeDataString(ns)}";
        }
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<JobPodsResponse>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    /// <summary>
    /// Watches a pod's status via Server-Sent Events until completion.
    /// </summary>
    /// <param name="podName">The name of the pod to watch.</param>
    /// <param name="ns">Optional namespace override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of pod status events.</returns>
    public async IAsyncEnumerable<PodStatusEvent> WatchPodAsync(
        string podName, 
        string? ns = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = $"/pods/{Uri.EscapeDataString(podName)}/watch";
        if (!string.IsNullOrEmpty(ns))
        {
            url += $"?ns={Uri.EscapeDataString(ns)}";
        }
        
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(line))
                continue;
            
            if (!line.StartsWith("data: "))
                continue;
            
            var data = line[6..]; // Remove "data: " prefix
            
            if (data == "[DONE]")
                yield break;
            
            var statusEvent = JsonSerializer.Deserialize<PodStatusEvent>(data, JsonOptions);
            if (statusEvent != null)
            {
                yield return statusEvent;
            }
        }
    }

    /// <summary>
    /// Watches a pod and waits for it to reach a terminal state (Succeeded or Failed).
    /// </summary>
    /// <param name="podName">The name of the pod to watch.</param>
    /// <param name="ns">Optional namespace override.</param>
    /// <param name="onStatusUpdate">Optional callback for status updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final pod status event.</returns>
    public async Task<PodStatusEvent> WaitForPodCompletionAsync(
        string podName,
        string? ns = null,
        Action<PodStatusEvent>? onStatusUpdate = null,
        CancellationToken cancellationToken = default)
    {
        PodStatusEvent? lastEvent = null;
        
        await foreach (var statusEvent in WatchPodAsync(podName, ns, cancellationToken))
        {
            lastEvent = statusEvent;
            onStatusUpdate?.Invoke(statusEvent);
            
            if (statusEvent.Phase is "Succeeded" or "Failed")
            {
                return statusEvent;
            }
        }
        
        return lastEvent ?? throw new InvalidOperationException("No status events received");
    }

    /// <summary>
    /// Gets logs from a specific pod.
    /// </summary>
    /// <param name="podName">The name of the pod.</param>
    /// <param name="ns">Optional namespace override.</param>
    /// <param name="container">Optional container name for multi-container pods.</param>
    /// <param name="previous">Whether to get logs from previous container instance.</param>
    /// <param name="tailLines">Number of lines from the end to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pod logs response.</returns>
    public async Task<PodLogsResponse> GetPodLogsAsync(
        string podName,
        string? ns = null,
        string? container = null,
        bool? previous = null,
        int? tailLines = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(ns))
            queryParams.Add($"ns={Uri.EscapeDataString(ns)}");
        if (!string.IsNullOrEmpty(container))
            queryParams.Add($"container={Uri.EscapeDataString(container)}");
        if (previous.HasValue)
            queryParams.Add($"previous={previous.Value.ToString().ToLowerInvariant()}");
        if (tailLines.HasValue)
            queryParams.Add($"tailLines={tailLines.Value}");
        
        var url = $"/pods/{Uri.EscapeDataString(podName)}/logs";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<PodLogsResponse>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}

