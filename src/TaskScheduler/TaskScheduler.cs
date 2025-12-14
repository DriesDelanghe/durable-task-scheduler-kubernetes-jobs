using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Common.Models;
using System.Linq;

namespace TaskScheduler;

public static class TaskScheduler
{
    [Function(nameof(TaskScheduler))]
    public static async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(TaskScheduler));
        logger.LogInformation("Starting Kubernetes job orchestration with fan-out/fan-in pattern");

        const int numberOfJobs = 10;
        
        // Fan-out: Start all jobs in parallel
        logger.LogInformation("Starting {Count} parallel Kubernetes jobs", numberOfJobs);
        var tasks = new List<Task<JobResult>>();
        
        for (int i = 0; i < numberOfJobs; i++)
        {
            var jobNumber = i + 1;
            logger.LogInformation("Queuing job {JobNumber}/{TotalJobs}", jobNumber, numberOfJobs);
            
            var task = context.CallActivityAsync<JobResult>(
                nameof(ExecuteKubernetesJobWithResultActivity),
                new CreateJobRequest());
            
            tasks.Add(task);
        }

        // Fan-in: Wait for all jobs to complete
        logger.LogInformation("Waiting for all {Count} jobs to complete", numberOfJobs);
        var results = await Task.WhenAll(tasks);

        // Aggregate results
        var totalTasks = results.Sum(r => r.TotalTasks);
        var successfulTasks = results.Sum(r => r.SuccessfulTasks);
        var failedTasks = results.Sum(r => r.FailedTasks);
        
        logger.LogInformation(
            "All {Count} jobs completed. Total tasks: {TotalTasks}, Successful: {SuccessfulTasks}, Failed: {FailedTasks}",
            numberOfJobs,
            totalTasks,
            successfulTasks,
            failedTasks);

        var summary = $"Completed {numberOfJobs} jobs: {totalTasks} total tasks ({successfulTasks} successful, {failedTasks} failed)";
        
        return summary;
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