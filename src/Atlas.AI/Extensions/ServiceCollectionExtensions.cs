using Atlas.AI.Providers;
using Atlas.Core.Interfaces;

namespace Atlas.AI.Extensions;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAtlasAI(
        this IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddSingleton<IAIProvider, OllamaAIProvider>();

        return services;
    }
}