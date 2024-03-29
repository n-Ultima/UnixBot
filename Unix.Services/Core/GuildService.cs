﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Unix.Common;
using Unix.Data;
using Unix.Data.Models.Core;
using Unix.Services.Core.Abstractions;
using Unix.Services.GatewayEventHandlers;
using Unix.Services.GatewayEventHandlers.ModerationEventHandlers;


namespace Unix.Services.Core;

public class GuildService : UnixService, IGuildService
{
    public GuildService(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <inheritdoc />
    public async Task CreateGuildConfigurationAsync(Snowflake guildId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guildConfig != null)
            {
                throw new Exception("Guild configuration for that ID already exists.");
            }

            unixContext.GuildConfigurations.Add(new GuildConfiguration
            {
                Id = guildId
            });
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<GuildConfiguration> FetchGuildConfigurationAsync(Snowflake guildId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.GuildConfigurations
                .FindAsync(guildId);
        }
    }

    /// <inheritdoc />
    public async Task ModifyGuildModLogChannelIdAsync(Snowflake guildId, Snowflake modlogChannelId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guild = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guild == null)
                throw new Exception("Guild should be configured using the `configure-guild` command first.");
            guild.ModLogChannelId = modlogChannelId;
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task ModifyGuildMessageLogChannelIdAsync(Snowflake guildId, Snowflake messageLogChannelId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guild = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guild == null)
                throw new Exception("Guild should be configured using the `configure-guild` command first.");
            guild.MessageLogChannelId = messageLogChannelId;
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task ModifyGuildAutomodAsync(Snowflake guildId, bool automodEnabled)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guild = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guild == null)
                throw new Exception("Guild should be configured using the `configure-guild` command first.");
            if (guild.AutomodEnabled == automodEnabled)
                throw new Exception($"Automod enabled is already {guild.AutomodEnabled}");
            guild.AutomodEnabled = automodEnabled;
            await unixContext.SaveChangesAsync();
            var msgHandle = scope.ServiceProvider.GetRequiredService<MessageCreateHandler>();
            if (msgHandle.GuildProcessMessages.TryGetValue(guildId, out _))
            {
                msgHandle.GuildProcessMessages[guildId] = automodEnabled;
            }
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task ModifyGuildMiscellaneousLogChannelIdAsync(Snowflake guildId, Snowflake miscChannelId)
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

            guild.MiscellaneousLogChannelId = miscChannelId;
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task ModifyGuildRequiredRoleAsync(Snowflake guildId, Snowflake requiredRoleId)
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

            guildConfig.RequiredRoleToUse = requiredRoleId;
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task ModifyGuildPhishermanApiKeyAsync(Snowflake guildId, string apiKey)
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

            guildConfig.PhishermanApiKey = apiKey;
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task AddBannedTermAsync(Snowflake guildId, string bannedTerm)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guildConfig == null)
            {
                throw new Exception("Guild should be configured with configure-guild first.");
            }

            if (guildConfig.BannedTerms.Contains(bannedTerm))
            {
                throw new Exception("That term already is already banned.");
            }
            guildConfig.BannedTerms.Add(bannedTerm);
            unixContext.Update(guildConfig);
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task RemoveBannedTermAsync(Snowflake guildId, string bannedTerm)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guildConfig == null)
            {
                throw new Exception("Guild should be configured with configure-guild first.");
            }
            if (!guildConfig.BannedTerms.Contains(bannedTerm))
            {
                throw new Exception("That term already is not currently banned.");
            }
            guildConfig.BannedTerms.Remove(bannedTerm);
            unixContext.Update(guildConfig);
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task AddWhitelistedInviteAsync(Snowflake guildId, Snowflake inviteGuildId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guildConfig == null)
            {
                throw new Exception("Guild should be configured with configure-guild first.");
            }

            if (guildConfig.WhitelistedInvites.Contains(inviteGuildId))
            {
                throw new Exception("The ID provided is already whitelisted.");
            }
            guildConfig.WhitelistedInvites.Add(inviteGuildId.RawValue);
            unixContext.Update(guildConfig);
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task RemoveWhitelistedInviteAsync(Snowflake guildId, Snowflake inviteGuildId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guildConfig == null)
            {
                throw new Exception("Guild should be configured with configure-guild first.");
            }
            if (!guildConfig.WhitelistedInvites.Contains(inviteGuildId))
            {
                throw new Exception("The ID provided is not currently whitelisted.");
            }
            guildConfig.WhitelistedInvites.Remove(inviteGuildId.RawValue);
            unixContext.Update(guildConfig);
            await unixContext.SaveChangesAsync();
        }
    }
    /// <inheritdoc />
    public async Task AddSelfAssignableRoleAsync(Snowflake guildId, Snowflake roleId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guildConfig == null)
            {
                throw new Exception("Guild should be configured with configure-guild first.");
            }

            if (guildConfig.SelfAssignableRoles.Contains(roleId.RawValue))
            {
                throw new Exception("That role is already marked as self assignable.");
            }
            guildConfig.SelfAssignableRoles.Add(roleId.RawValue);
            unixContext.Update(guildConfig);
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task RemoveSelfAssignableRoleAsync(Snowflake guildId, Snowflake roleId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guildConfig == null)
            {
                throw new Exception("Guild should be configured with configure-guild first.");
            }
            if (!guildConfig.SelfAssignableRoles.Contains(roleId.RawValue))
            {
                throw new Exception("That role is not currently marked as self assignable.");
            }
            guildConfig.SelfAssignableRoles.Remove(roleId.RawValue);
            unixContext.Update(guildConfig);
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task AddAutoRoleAsync(Snowflake guildId, Snowflake roleId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guildConfig == null)
            {
                throw new Exception("Guild should be configured with configure-guild first.");
            }

            if (guildConfig.AutoRoles.Contains(roleId.RawValue))
            {
                throw new Exception("That role is already marked as an autorole.");
            }
            guildConfig.AutoRoles.Add(roleId.RawValue);
            unixContext.Update(guildConfig);
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAutoRoleAsync(Snowflake guildId, Snowflake roleId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            if (guildConfig == null)
            {
                throw new Exception("Guild should be configured with configure-guild first.");
            }

            if (!guildConfig.AutoRoles.Contains(roleId.RawValue))
            {
                throw new Exception("That role is not currently marked as an autorole.");
            }

            guildConfig.AutoRoles.Remove(roleId.RawValue);
            unixContext.Update(guildConfig);
            await unixContext.SaveChangesAsync();
        }
    }
}