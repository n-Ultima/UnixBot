using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Common;
using Unix.Services.Core;
using Unix.Services.Extensions;

namespace Unix.Services.GatewayEventHandlers;

public class InteractionHandler : UnixService
{
    private readonly OwnerService _ownerService;
    private readonly GuildService _guildService;
    private readonly ModerationService _moderationService;
    
    public InteractionHandler(IServiceProvider serviceProvider, OwnerService ownerService, GuildService guildService, ModerationService moderationService) : base(serviceProvider)
    {
        _ownerService = ownerService;
        _guildService = guildService;
        _moderationService = moderationService;
    }

    protected async override ValueTask OnInteractionReceived(InteractionReceivedEventArgs eventArgs)
    {
        if (eventArgs.Interaction.Type != InteractionType.ApplicationCommand)
        {
            return;
        }

        if (eventArgs.Interaction is not ISlashCommandInteraction slashCommandInteraction)
        {
            return;
        }

        if (!eventArgs.GuildId.HasValue)
        {
            return;
        }

        var guildConfig = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId.Value);
        if (guildConfig == null)
        {
            await eventArgs.SendEphmeralErrorAsync($"You must request access with Unix before use. Please see http://www.ultima.one/unix");
            return;
        }
        switch (slashCommandInteraction.CommandName)
        {
            case "ping":
                var dateTime = DateTimeOffset.UtcNow - eventArgs.Interaction.CreatedAt();
                var heartbeatLatency = eventArgs.Interaction.GetGatewayClient().ApiClient.Heartbeater.Latency;
                var builder = new StringBuilder();
                if (!heartbeatLatency.HasValue)
                {
                    builder.Append($"🏓 Pong!\nShard Latency: {Bot.GetShard(eventArgs.GuildId.Value).Heartbeater.Latency.Value.Milliseconds} ms\nMessage Latency: {dateTime.Milliseconds} ms");
                }
                else
                {
                    builder.Append($"🏓 Pong!\nDirect API Latency: {heartbeatLatency.Value.Milliseconds} ms\nShard Latency: {Bot.GetShard(eventArgs.GuildId.Value).Heartbeater.Latency.Value.Milliseconds} ms\nMessage Latency: {dateTime.Milliseconds} ms");
                }

                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithContent(builder.ToString()));
                break;
            case "guild-count":
                if (!Bot.OwnerIds.Contains(eventArgs.Member.Id))
                {
                    await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                        .WithIsEphemeral()
                        .WithContent("You must be a bot owner to execute this command."));
                    break;
                }

                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithContent(Bot.GetGuilds().Count.ToString()));
                break;
            case "configure-muterole":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var muteRole = slashCommandInteraction.Entities.Roles.First().Value;
                await _guildService.ModifyGuildMuteRoleIdAsync(eventArgs.GuildId.Value, muteRole.Id);
                await eventArgs.SendSuccessAsync($"Muterole configured to **{muteRole.Name}**");
                break;
            case "configure-modrole":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var modRole = slashCommandInteraction.Entities.Roles.First().Value;
                await _guildService.ModifyGuildModRoleAsync(eventArgs.GuildId.Value, modRole.Id);
                await eventArgs.SendSuccessAsync($"Moderator role set to **{modRole.Name}**");
                break;
            case "configure-adminrole":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var adminRole = slashCommandInteraction.Entities.Roles.First().Value;
                await _guildService.ModifyGuildAdminRoleAsync(eventArgs.GuildId.Value, adminRole.Id);
                await eventArgs.SendSuccessAsync($"Administrator role set to **{adminRole.Name}**");
                break;
            case "configure-modlog":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var modChannel = slashCommandInteraction.Entities.Channels.First().Value;
                await _guildService.ModifyGuildModLogChannelIdAsync(eventArgs.GuildId.Value, modChannel.Id);
                await eventArgs.SendSuccessAsync($"Moderation actions taken with Unix will now be logged to {Mention.Channel(modChannel.Id)}");
                break;
            case "configure-messagelog":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var messageChannel = slashCommandInteraction.Entities.Channels.First().Value;
                await _guildService.ModifyGuildMessageLogChannelIdAsync(eventArgs.GuildId.Value, messageChannel.Id);
                await eventArgs.SendSuccessAsync($"Message updates and deletions will now be logged to {Mention.Channel(messageChannel.Id)}");
                break;
            case "configure-automod":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var boolOption = slashCommandInteraction.Options.TryGetValue("enabled", out var enabled);
                if (!boolOption)
                {
                    break;
                }

                var isEnabled = (bool) enabled.Value;
                try
                {
                    await _guildService.ConfigureGuildAutomodAsync(eventArgs.GuildId.Value, isEnabled);
                    await eventArgs.SendSuccessAsync($"Automod is enabled: **{isEnabled}**");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                }
                break;
            case "configure-spam":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var spamAmtOption = slashCommandInteraction.Options.TryGetValue("amount", out var amount);
                if (!spamAmtOption)
                {
                    break;
                }

                var actualSpamAmount = (int) amount.Value;
                await _guildService.ModifyGuildSpamThresholdAsync(eventArgs.GuildId.Value, actualSpamAmount);
                await eventArgs.SendSuccessAsync($"If a user sends more than `{actualSpamAmount}` in `3` seconds, they will be warned.");
                break;
            case "add-banned-term":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var bannedTermOption = slashCommandInteraction.Options.GetValueOrDefault("amount")?.Value as string;
                try
                {
                    await _guildService.AddBannedTermAsync(eventArgs.GuildId.Value, bannedTermOption);
                    await eventArgs.SendSuccessAsync($"Users without the `Moderator` or `Administrator` permission will now be warned for using `{bannedTermOption}`");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                }
                break;
            case "remove-banned-term":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var removedBannedTermOption = slashCommandInteraction.Options.GetValueOrDefault("amount")?.Value as string;
                try
                {
                    await _guildService.RemoveBannedTermAsync(eventArgs.GuildId.Value, removedBannedTermOption);
                    await eventArgs.SendSuccessAsync($"Users without the `Moderator` or `Administrator` permission will no longer be warned for using `{removedBannedTermOption}`");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                }

                break;
            case "add-whitelisted-guild":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var whitelistSnowflake = slashCommandInteraction.Options.GetValueOrDefault("id")?.Value as string;
                if (!Snowflake.TryParse(whitelistSnowflake, out var realWhitelistSnowflake))
                {
                    await eventArgs.SendEphmeralErrorAsync("Invalid snowflake provided.");
                    break;
                }

                var whitelistGuild = await Bot.FetchGuildAsync(realWhitelistSnowflake);
                if (whitelistGuild == null)
                {
                    await eventArgs.SendEphmeralErrorAsync("The ID provided is not valid.");
                    break;
                }

                try
                {
                    await _guildService.AddWhitelistedInviteAsync(eventArgs.GuildId.Value, realWhitelistSnowflake);
                    await eventArgs.SendSuccessAsync($"Invites pointing towards **{whitelistGuild.Name}** will no longer be deleted.");
                    break;
                }
                catch(Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                }

                break;
            case "remove-whitelisted-guild":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var blacklistSnowflake = slashCommandInteraction.Options.GetValueOrDefault("id")?.Value as string;
                if (!Snowflake.TryParse(blacklistSnowflake, out var realBlacklistSnowflake))
                {
                    await eventArgs.SendEphmeralErrorAsync("Invalid snowflake provided.");
                    break;
                }

                var guild = await Bot.FetchGuildAsync(realBlacklistSnowflake);
                if (guild == null)
                {
                    await eventArgs.SendEphmeralErrorAsync("The ID provided is not valid.");
                    break;
                }

                try
                {
                    await _guildService.AddWhitelistedInviteAsync(eventArgs.GuildId.Value, realBlacklistSnowflake);
                    await eventArgs.SendSuccessAsync($"Invites pointing towards **{guild.Name}** will now be deleted.");
                    break;
                }
                catch(Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                }

                break;
                
        }
    }
}