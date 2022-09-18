using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Unix.Common;
using Unix.Data;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.CoreEventHandlers;

public class ReadyEventHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly ILogger<ReadyEventHandler> _logger;
    public UnixConfiguration UnixConfig = new();

    public ReadyEventHandler(IGuildService guildService, ILogger<ReadyEventHandler> logger, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _guildService = guildService;
        _logger = logger;
    }

    protected override async ValueTask OnReady(ReadyEventArgs e)
    {
        await Bot.SetPresenceAsync(UserStatus.Online, new LocalActivity("slash commands", ActivityType.Watching));
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var allowedGuildIds = await unixContext.GuildConfigurations.Select(x => x.Id).ToListAsync();
            var unauthorizedGuilds = e.GuildIds.Except(allowedGuildIds);
            if (unauthorizedGuilds.Any() && UnixConfig.PrivelegedMode)
            {
                _logger.LogWarning("Guilds were found that Unix isn't authorized to operate in. IDs: [{guildIds}]", unauthorizedGuilds.Humanize());
                // Now, we leave each of the guilds that Unix shouldn't be in.
                foreach (var guild in unauthorizedGuilds)
                {
                    var g = await Bot.FetchGuildAsync(guild);
                    await Bot.LeaveGuildAsync(guild, new DefaultRestRequestOptions
                    {
                        Reason = "Unauthorized. Join the Unix server to request access."
                    });
                    var gName = g.Name;
                    _logger.LogInformation("Left guild {g} due to lack of authorization.", gName);
                }
            }

            if (!UnixConfig.PrivelegedMode)
            {
                foreach (var guild in e.GuildIds)
                {
                    if (!allowedGuildIds.Contains(guild))
                    {
                        _logger.LogInformation("Guild found that doesn't have a configuration created Creating. ID: {id}", guild);
                        await _guildService.CreateGuildConfigurationAsync(guild);
                        _logger.LogInformation("Created!");
                    }
                }
            }

        }
    }
}