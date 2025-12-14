using k8s;
using Common.Models;
using k8sController.Models;

namespace k8sController.Handlers;

public static class GetJobPodsHandler
{
    public static async Task<IResult> HandleAsync(
        string jobName,
        IKubernetes client,
        IConfiguration configuration,
        string? ns,
        CancellationToken cancellationToken)
    {
        var settings = configuration.GetSection("Kubernetes").Get<KubernetesSettings>() 
            ?? new KubernetesSettings();
        
        var targetNamespace = ns ?? settings.DefaultNamespace;
        
        try
        {
            var pods = await client.CoreV1.ListNamespacedPodAsync(
                targetNamespace,
                labelSelector: $"job-name={jobName}",
                cancellationToken: cancellationToken);
            
            var podInfos = pods.Items.Select(p => new PodInfo
            {
                PodName = p.Metadata.Name,
                Namespace = p.Metadata.NamespaceProperty,
                Phase = p.Status.Phase,
                StartTime = p.Status.StartTime,
                NodeName = p.Spec.NodeName
            }).ToList();
            
            return Results.Ok(new JobPodsResponse
            {
                JobName = jobName,
                Namespace = targetNamespace,
                Pods = podInfos
            });
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Results.NotFound(new { message = $"No pods found for job '{jobName}' in namespace '{targetNamespace}'" });
        }
    }
}

