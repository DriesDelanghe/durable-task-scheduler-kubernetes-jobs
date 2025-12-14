using Microsoft.Extensions.DependencyInjection;

namespace Common;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the K8sControllerClient to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseAddress">The base address of the k8sController API.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddK8sControllerClient(
        this IServiceCollection services, 
        string baseAddress)
    {
        services.AddHttpClient<K8sControllerClient>(client =>
        {
            client.BaseAddress = new Uri(baseAddress);
        });
        
        return services;
    }

    /// <summary>
    /// Adds the K8sControllerClient to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureClient">Action to configure the HttpClient.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddK8sControllerClient(
        this IServiceCollection services,
        Action<HttpClient> configureClient)
    {
        services.AddHttpClient<K8sControllerClient>(configureClient);
        
        return services;
    }
}

