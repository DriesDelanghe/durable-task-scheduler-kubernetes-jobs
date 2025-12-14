using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Common.Models;

namespace TaskScheduler;

public static class TaskScheduler
{
    [Function(nameof(TaskScheduler))]
    public static async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(TaskScheduler));
        logger.LogInformation("Starting Kubernetes job orchestration");

        // Call the activity to execute the Kubernetes job and get logs
        var jobLogs = await context.CallActivityAsync<JobResult>(
            nameof(ExecuteKubernetesJobWithResultActivity),
            new CreateJobRequest());

        logger.LogInformation("Kubernetes job completed. Result: {Summary}", jobLogs?.Summary ?? string.Empty);
        
        return jobLogs?.Summary ?? string.Empty;
    }

    [Function(nameof(ExecuteKubernetesJobWithResultActivity))]
    public static async Task<JobResult> ExecuteKubernetesJobWithResultActivity(
        FunctionContext executionContext,
        [ActivityTrigger] CreateJobRequest request)
    {
        var logger = executionContext.GetLogger(nameof(ExecuteKubernetesJobWithResultActivity));
        var serviceProvider = executionContext.InstanceServices;
        
        var orchestrator = serviceProvider.GetService<KubernetesJobOrchestrator>()
            ?? throw new InvalidOperationException($"Unable to resolve {nameof(KubernetesJobOrchestrator)} from service provider");
        
        logger.LogInformation("Executing Kubernetes job activity with structured result");
        
        var result = await orchestrator.ExecuteJobAndGetResultAsync(new CreateJobRequest());
        
        logger.LogInformation(
            "Kubernetes job activity completed. Result: {Summary}",
            result.Summary);
        
        return result;
    }

    [Function("TaskScheduler_HttpStart")]
    public static async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("TaskScheduler_HttpStart");

        // Function input comes from the request content.
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(TaskScheduler));

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // Returns an HTTP 202 response with an instance management payload.
        // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }
}