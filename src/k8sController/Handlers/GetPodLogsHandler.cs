using k8s;
using Common.Models;
using k8sController.Models;

namespace k8sController.Handlers;

public static class GetPodLogsHandler
{
    public static async Task<IResult> HandleAsync(
        string podName,
        IKubernetes client,
        IConfiguration configuration,
        string? ns,
        string? container,
        bool? previous,
        int? tailLines,
        CancellationToken cancellationToken)
    {
        var settings = configuration.GetSection("Kubernetes").Get<KubernetesSettings>() 
            ?? new KubernetesSettings();
        
        var targetNamespace = ns ?? settings.DefaultNamespace;
        
        try
        {
            using var logStream = await client.CoreV1.ReadNamespacedPodLogAsync(
                podName,
                targetNamespace,
                container: container,
                previous: previous,
                tailLines: tailLines,
                cancellationToken: cancellationToken);
            
            using var reader = new StreamReader(logStream);
            var logs = await reader.ReadToEndAsync(cancellationToken);
            
            return Results.Ok(new PodLogsResponse
            {
                PodName = podName,
                Namespace = targetNamespace,
                Container = container,
                Logs = logs,
                RetrievedAt = DateTime.UtcNow
            });
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Results.NotFound(new { message = $"Pod '{podName}' not found in namespace '{targetNamespace}'" });
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return Results.BadRequest(new { message = ex.Response.Content });
        }
    }
}

