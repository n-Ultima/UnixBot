using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using Unix.Common;
using Unix.Data;
using Unix.Data.Models.Core;
using Unix.Services.Core;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.Core;

public class OwnerService : UnixService, IOwnerService
{
    private readonly GuildService _guildService;
    public UnixConfiguration UnixConfig = new();
    public OwnerService(IServiceProvider serviceProvider, GuildService guildService)
        : base(serviceProvider)
    {
        _guildService = guildService;
    }

    public static List<Snowflake> WhitelistedGuilds = new();

    /// <inheritdoc />
    public async Task ConfigureGuildAsync(Snowflake guildId, Snowflake modLogChannelId, Snowflake messageLogChannelId, Snowflake modRoleId, Snowflake adminRoleId, bool automodEnabled)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guild = await _guildService.FetchGuildConfigurationAsync(guildId);
            if (guild != null)
                throw new Exception("This guild has already been configured.");
            unixContext.GuildConfigurations.Add(new GuildConfiguration
            {
                Id = guildId,
                ModLogChannelId = modLogChannelId,
                MessageLogChannelId = messageLogChannelId,
                ModeratorRoleId = modRoleId,
                AdministratorRoleId = adminRoleId,
                AutomodEnabled = automodEnabled
            });
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task BlacklistGuildAsync(Snowflake guildId)
    {
        if (!UnixConfig.PrivelegedMode)
        {
            throw new Exception("You cannot disallow guilds while the `PrivelegedMode` option is set to false.");
        }
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guild = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guild == null)
                throw new Exception("That guild isn't allowed to use Unix.");
            unixContext.GuildConfigurations.Remove(guild);
            WhitelistedGuilds.Remove(guild.Id);
            await unixContext.SaveChangesAsync();
        }
    }

}