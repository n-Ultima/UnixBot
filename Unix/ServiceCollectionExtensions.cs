using Microsoft.Extensions.DependencyInjection;
using Unix.Data.Migrations;
using Unix.Services;
using Unix.Services.Core;
using Unix.Services.Core.Abstractions;

namespace Unix;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUnixServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGuildService, GuildService>();
        serviceCollection.AddSingleton<IModerationService, ModerationService>();
        serviceCollection.AddSingleton<IOwnerService, OwnerService>();
        serviceCollection.AddSingleton<IPhishermanService, PhishermanService>();
        serviceCollection.AddSingleton<IReminderService, ReminderService>();
        return serviceCollection;
    }
}