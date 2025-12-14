using System.Text.Json;
using k8s;
using k8s.Models;
using Common.Models;
using k8sController.Models;

namespace k8sController.Handlers;

public static class WatchPodHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task HandleAsync(
        string podName,
        IKubernetes client,
        IConfiguration configuration,
        HttpContext httpContext,
        string? ns,
        CancellationToken cancellationToken)
    {
        var settings = configuration.GetSection("Kubernetes").Get<KubernetesSettings>() 
            ?? new KubernetesSettings();
        
        var targetNamespace = ns ?? settings.DefaultNamespace;
        
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";
        
        var completionPhases = new HashSet<string> { "Succeeded", "Failed" };
        
        try
        {
            // First, get current pod state
            var pod = await client.CoreV1.ReadNamespacedPodAsync(podName, targetNamespace, cancellationToken: cancellationToken);
            
            await SendSseEventAsync(httpContext, new PodStatusEvent
            {
                PodName = pod.Metadata.Name,
                Phase = pod.Status.Phase,
                Message = pod.Status.Message,
                Reason = pod.Status.Reason,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);
            
            // Check if already in a terminal state
            if (completionPhases.Contains(pod.Status.Phase))
            {
                await SendSseEventAsync(httpContext, "[DONE]", cancellationToken);
                return;
            }
            
            // Start watching for changes
            var watchResponse = client.CoreV1.ListNamespacedPodWithHttpMessagesAsync(
                targetNamespace,
                fieldSelector: $"metadata.name={podName}",
                watch: true,
                cancellationToken: cancellationToken);
            
            await foreach (var (eventType, watchedPod) in watchResponse.WatchAsync<V1Pod, V1PodList>(cancellationToken: cancellationToken))
            {
                var statusEvent = new PodStatusEvent
                {
                    PodName = watchedPod.Metadata.Name,
                    Phase = watchedPod.Status.Phase,
                    Message = watchedPod.Status.Message,
                    Reason = watchedPod.Status.Reason,
                    EventType = eventType.ToString(),
                    Timestamp = DateTime.UtcNow
                };
                
                await SendSseEventAsync(httpContext, statusEvent, cancellationToken);
                
                // Complete the stream if pod reached a terminal phase
                if (completionPhases.Contains(watchedPod.Status.Phase))
                {
                    await SendSseEventAsync(httpContext, "[DONE]", cancellationToken);
                    break;
                }
            }
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsync($"Pod '{podName}' not found in namespace '{targetNamespace}'", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected, this is expected
        }
    }

    private static async Task SendSseEventAsync<T>(HttpContext context, T data, CancellationToken cancellationToken)
    {
        string message;
        if (data is string str)
        {
            message = str;
        }
        else
        {
            message = JsonSerializer.Serialize(data, JsonOptions);
        }
        await context.Response.WriteAsync($"data: {message}\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
}

