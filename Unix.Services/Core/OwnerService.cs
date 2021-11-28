using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using Unix.Data;
using Unix.Data.Models.Core;
using Unix.Services.Core;

namespace Unix.Services
{
    public class OwnerService : UnixService
    {
        private readonly GuildService _guildService;

        public OwnerService(IServiceProvider serviceProvider, GuildService guildService)
            : base(serviceProvider)
        {
            _guildService = guildService;
        }

        public static List<Snowflake> WhitelistedGuilds = new();
        public async Task<IEnumerable<Snowflake>> FetchWhitelistedGuildsAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var guilds = await unixContext.GuildConfigurations.Select(x => x.Id).ToListAsync();
                return guilds;
            }
        }
        
        public async Task ConfigureGuildAsync(Snowflake guildId, Snowflake muteRoleId, Snowflake modLogChannelId, Snowflake messageLogChannelId,Snowflake modRoleId, Snowflake adminRoleId, bool automodEnabled)
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
                    MuteRoleId = muteRoleId,
                    ModLogChannelId = modLogChannelId,
                    MessageLogChannelId = messageLogChannelId,
                    ModeratorRoleId = modRoleId,
                    AdministratorRoleId = adminRoleId,
                    AutomodEnabled = automodEnabled
                });
                await unixContext.SaveChangesAsync();
            }
        }
        public async Task BlacklistGuildAsync(Snowflake guildId)
        {
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
}