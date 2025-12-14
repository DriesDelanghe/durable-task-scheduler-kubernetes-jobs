using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common;
using TaskScheduler;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register K8sControllerClient
// The base address should be configured via environment variable or configuration
var k8sControllerBaseAddress = builder.Configuration["K8sController:BaseAddress"] 
    ?? Environment.GetEnvironmentVariable("K8S_CONTROLLER_BASE_ADDRESS")
    ?? "http://localhost:5263";

builder.Services.AddK8sControllerClient(k8sControllerBaseAddress);

// Register KubernetesJobOrchestrator
builder.Services.AddScoped<KubernetesJobOrchestrator>();

builder.Build().Run();
