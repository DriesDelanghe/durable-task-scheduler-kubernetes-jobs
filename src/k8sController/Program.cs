using k8s;
using Common.Models;
using k8sController.Handlers;
using k8sController.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Register Kubernetes client for in-cluster configuration
builder.Services.AddSingleton<IKubernetes>(_ =>
{
    var config = KubernetesClientConfiguration.InClusterConfig();
    return new Kubernetes(config);
});

// Register configuration
builder.Services.Configure<KubernetesSettings>(
    builder.Configuration.GetSection("Kubernetes"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// POST /jobs - Create a new Kubernetes job for the ExampleWorker
app.MapPost("/jobs", CreateJobHandler.HandleAsync)
    .WithName("CreateJob")
    .WithDescription("Creates a new Kubernetes job running the ExampleWorker")
    .Produces<CreateJobResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

// GET /pods/{podName}/watch - Stream pod status updates via SSE
app.MapGet("/pods/{podName}/watch", WatchPodHandler.HandleAsync)
    .WithName("WatchPod")
    .WithDescription("Streams pod status updates via Server-Sent Events until the pod reaches Succeeded or Failed state")
    .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
    .ProducesProblem(StatusCodes.Status404NotFound);

// GET /pods/{podName}/logs - Get logs from a specific pod
app.MapGet("/pods/{podName}/logs", GetPodLogsHandler.HandleAsync)
    .WithName("GetPodLogs")
    .WithDescription("Retrieves logs from a specific pod")
    .Produces<PodLogsResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status400BadRequest);

// GET /jobs/{jobName}/pods - Get pods for a specific job
app.MapGet("/jobs/{jobName}/pods", GetJobPodsHandler.HandleAsync)
    .WithName("GetJobPods")
    .WithDescription("Gets all pods created by a specific job")
    .Produces<JobPodsResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

app.Run();
