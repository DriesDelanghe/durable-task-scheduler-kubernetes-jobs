using k8s;
using k8s.Models;
using Common.Models;
using k8sController.Models;

namespace k8sController.Handlers;

public static class CreateJobHandler
{
    public static async Task<IResult> HandleAsync(
        IKubernetes client,
        IConfiguration configuration,
        CreateJobRequest? request)
    {
        var settings = configuration.GetSection("Kubernetes").Get<KubernetesSettings>() 
            ?? new KubernetesSettings();
        
        var targetNamespace = request?.Namespace ?? settings.DefaultNamespace;
        var workerImage = settings.WorkerImage;
        
        var jobName = $"example-worker-{Guid.NewGuid():N}";
        
        var job = new V1Job
        {
            ApiVersion = "batch/v1",
            Kind = "Job",
            Metadata = new V1ObjectMeta
            {
                Name = jobName,
                NamespaceProperty = targetNamespace,
                Labels = new Dictionary<string, string>
                {
                    ["app"] = "example-worker",
                    ["managed-by"] = "k8s-controller"
                }
            },
            Spec = new V1JobSpec
            {
                BackoffLimit = 0, // No retries
                TtlSecondsAfterFinished = 3600, // Clean up after 1 hour
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string>
                        {
                            ["app"] = "example-worker",
                            ["job-name"] = jobName
                        }
                    },
                    Spec = new V1PodSpec
                    {
                        RestartPolicy = "Never",
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "worker",
                                Image = workerImage,
                                ImagePullPolicy = "IfNotPresent"
                            }
                        }
                    }
                }
            }
        };

        try
        {
            var createdJob = await client.BatchV1.CreateNamespacedJobAsync(job, targetNamespace);
            
            return Results.Ok(new CreateJobResponse
            {
                JobName = createdJob.Metadata.Name,
                Namespace = createdJob.Metadata.NamespaceProperty,
                JobUid = createdJob.Metadata.Uid,
                LabelSelector = $"job-name={jobName}",
                CreatedAt = createdJob.Metadata.CreationTimestamp
            });
        }
        catch (k8s.Autorest.HttpOperationException ex)
        {
            return Results.Problem(
                detail: ex.Response.Content,
                statusCode: (int)ex.Response.StatusCode);
        }
    }
}

