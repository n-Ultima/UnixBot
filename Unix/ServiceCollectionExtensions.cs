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
        serviceCollection.AddSingleton<IGuildService>(x => x.GetRequiredService<GuildService>());
        serviceCollection.AddSingleton<IModerationService>(x => x.GetRequiredService<ModerationService>());
        serviceCollection.AddSingleton<IOwnerService>(x => x.GetRequiredService<OwnerService>());
        serviceCollection.AddSingleton<IPhishermanService>(x => x.GetRequiredService<PhishermanService>());
        serviceCollection.AddSingleton<IReminderService>(x => x.GetRequiredService<ReminderService>());
        serviceCollection.AddSingleton<ITagService>(x => x.GetRequiredService<TagService>());
        return serviceCollection;
    }
}