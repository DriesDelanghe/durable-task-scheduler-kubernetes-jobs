namespace k8sController.Models;

public class KubernetesSettings
{
    public string DefaultNamespace { get; set; } = "default";
    public string WorkerImage { get; set; } = "example-worker:latest";
}

