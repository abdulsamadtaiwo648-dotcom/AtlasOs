using Atlas.Core.Interfaces;
using Atlas.Core.Planning;
using Atlas.Core.Services;
using Atlas.Tools;
using Microsoft.Extensions.DependencyInjection;
namespace Atlas.Core.Extensions;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAtlasCore(
        this IServiceCollection services)
    {
        services.AddSingleton<IConversationStore, JsonConversationStore>();

        services.AddSingleton<IPlannerEngine, PlannerEngine>();

        services.AddSingleton<CommandDispatcher>();

        services.AddSingleton<IConversationService, ConversationService>();

        services.AddSingleton<IAtlasKernel, AtlasKernel>();

        return services;
    }
}