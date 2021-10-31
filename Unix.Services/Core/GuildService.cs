using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Unix.Common;
using Unix.Data;
using Unix.Data.Models.Core;
using Unix.Services.GatewayEventHandlers;

namespace Unix.Services.Core
{

    public static class C
    {
        private static readonly IServiceProvider ServiceProvider;
    }
    public class GuildService : UnixService
    {
        public GuildService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        public async Task<GuildConfiguration> FetchGuildConfigurationAsync(Snowflake guildId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                return await unixContext.GuildConfigurations
                    .FindAsync(guildId);
            }
        }

        public async Task ModifyGuildMuteRoleIdAsync(Snowflake guildId, Snowflake muteRoleId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var guild = await unixContext.GuildConfigurations
                    .FindAsync(guildId);
                if(guild == null)
                    throw new Exception("Guild should be configured using the `configure-guild` command first.");
                guild.MuteRoleId = muteRoleId;
                await unixContext.SaveChangesAsync();
            }
        }

        public async Task ModifyGuildModLogChannelIdAsync(Snowflake guildId, Snowflake modlogChannelId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var guild = await unixContext.GuildConfigurations
                    .FindAsync(guildId);
                if(guild == null)
                    throw new Exception("Guild should be configured using the `configure-guild` command first.");
                guild.ModLogChannelId = modlogChannelId;
                await unixContext.SaveChangesAsync();
            }
        }

        public async Task ModifyGuildMessageLogChannelIdAsync(Snowflake guildId, Snowflake messageLogChannelId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var guild = await unixContext.GuildConfigurations
                    .FindAsync(guildId);
                if(guild == null)
                    throw new Exception("Guild should be configured using the `configure-guild` command first.");
                guild.MessageLogChannelId = messageLogChannelId;
                await unixContext.SaveChangesAsync();
            }
        }
        

        public async Task ConfigureGuildAutomodAsync(Snowflake guildId, bool automodEnabled)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var guild = await unixContext.GuildConfigurations
                    .FindAsync(guildId);
                if(guild == null)
                    throw new Exception("Guild should be configured using the `configure-guild` command first.");
                if (guild.AutomodEnabled == automodEnabled)
                    throw new Exception($"Automod enabled is already {guild.AutomodEnabled}");
                guild.AutomodEnabled = automodEnabled;
                await unixContext.SaveChangesAsync();
            }
        }
        
        public async Task ModifyGuildPrefixAsync(Snowflake guildId, string newPrefix)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var guild = await unixContext.GuildConfigurations
                    .FindAsync(guildId);
                if (guild == null)
                    throw new Exception("Guild should be configured using the `configure-guild` command first.");
                guild.Prefix = newPrefix;
                await unixContext.SaveChangesAsync();
            }
        }

        public async Task ModifyGuildModRoleAsync(Snowflake guildId, Snowflake modRoleId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var guild = await unixContext.GuildConfigurations
                    .FindAsync(guildId);
                if (guild == null)
                {
                    throw new Exception("Guild should be configured using the `configure-guild` command first.");
                }

                guild.ModeratorRoleId = modRoleId;
                await unixContext.SaveChangesAsync();
            }
        }

        public async Task ModifyGuildAdminRoleAsync(Snowflake guildId, Snowflake adminRoleId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var guild = await unixContext.GuildConfigurations
                    .FindAsync(guildId);
                if (guild == null)
                {
                    throw new Exception("Guild should be configured using the `configure-guild` command first.");
                }

                guild.AdministratorRoleId = adminRoleId;
                await unixContext.SaveChangesAsync();
            }
        }
        public async Task<string> FetchGuildPrefixAsync(Snowflake guildId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                return await unixContext.GuildConfigurations
                    .Where(x => x.Id == guildId)
                    .Select(x => x.Prefix)
                    .SingleOrDefaultAsync();
            }
        }

        public async Task ModifyGuildSpamThresholdAsync(Snowflake guildId, int amount)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var guildConfig = await unixContext.GuildConfigurations
                    .FindAsync(guildId);
                if (guildConfig == null)
                {
                    throw new Exception("Guild should be configured using the `configure-guild` command first.");
                }

                guildConfig.AmountOfMessagesConsideredSpam = amount;
                await unixContext.SaveChangesAsync();
                SpamHandler.AmountOfMessages[guildConfig.Id] = amount;
            }
        }
        public async Task<List<IPrefix>> FetchIPrefixesAsync(Snowflake guildId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var results = await unixContext.GuildConfigurations
                    .Where(x => x.Id == guildId)
                    .Select(x => x.Prefix)
                    .SingleOrDefaultAsync();
                List<IPrefix> prefixes = new();
                prefixes.Add(new StringPrefix(results));
                prefixes.Add(new MentionPrefix(Bot.CurrentUser.Id));
                return prefixes;
            }
        }
    }
}