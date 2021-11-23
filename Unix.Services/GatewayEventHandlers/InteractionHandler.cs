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

        var guild = Bot.GetGuild(eventArgs.GuildId.Value);
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

                var blacklistGuild = await Bot.FetchGuildAsync(realBlacklistSnowflake);
                if (blacklistGuild == null)
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
            // the big one
            case "configure-guild":
                if (!Bot.OwnerIds.Contains(eventArgs.Member.Id))
                {
                    await eventArgs.SendEphmeralErrorAsync("You must be a bot owner to use this command.");
                    break;
                }
                // get the options.
                var guildIdString = slashCommandInteraction.Options.GetValueOrDefault("id")?.Value as string;
                var muteRoleString = slashCommandInteraction.Options.GetValueOrDefault("mute-role-id")?.Value as string;
                var modLogString = slashCommandInteraction.Options.GetValueOrDefault("modlog-channel-id")?.Value as string;
                var messageLogString = slashCommandInteraction.Options.GetValueOrDefault("messagelog-channel-id")?.Value as string;
                var modRoleString = slashCommandInteraction.Options.GetValueOrDefault("moderator-role-id")?.Value as string;
                var adminRoleString = slashCommandInteraction.Options.GetValueOrDefault("administrator-role-id")?.Value as string;
                var isAutomodEnabled = slashCommandInteraction.Options.TryGetValue("automod-enabled", out var sCommandInteraction);
                var autoModEnabled = (bool) sCommandInteraction.Value;
                if (!Snowflake.TryParse(guildIdString, out var realGuildId))
                {
                    await eventArgs.SendEphmeralErrorAsync($"Invalid snowflake provided for guild ID.");
                    break;
                }
                if (!Snowflake.TryParse(muteRoleString, out var realMuteRole))
                {
                    await eventArgs.SendEphmeralErrorAsync("Invalid snowflake provided for mute role ID.");
                    break;
                }
                if (!Snowflake.TryParse(modLogString, out var realModLog))
                {
                    await eventArgs.SendEphmeralErrorAsync("Invalid snowflake provided for mod log ID.");
                    break;
                }
                if (!Snowflake.TryParse(messageLogString, out var realMessageLog))
                {
                    await eventArgs.SendEphmeralErrorAsync("Invalid snowflake provided for message log ID.");
                    break;
                }
                if (!Snowflake.TryParse(modRoleString, out var realModRole))
                {
                    await eventArgs.SendEphmeralErrorAsync("Invalid snowflake provided for moderator role ID.");
                    break;
                }
                if (!Snowflake.TryParse(adminRoleString, out var realAdminRole))
                {
                    await eventArgs.SendEphmeralErrorAsync("Invalid snowflake provided for administrator role ID.");
                    break;
                }
                // configure the guild
                await eventArgs.SendSuccessAsync($"Successfully configured!");
                break;
            case "disallow-guild":
                if (!Bot.OwnerIds.Contains(eventArgs.Member.Id))
                {
                    await eventArgs.SendEphmeralErrorAsync("You must be a bot owner to use this command.");
                    break;
                }

                var id = slashCommandInteraction.Options.GetValueOrDefault("id")?.Value as string;
                if (!Snowflake.TryParse(id, out var disallowId))
                {
                    await eventArgs.SendEphmeralErrorAsync($"Invalid snowflake provided.");
                    break;
                }

                try
                {
                    await _ownerService.BlacklistGuildAsync(disallowId);
                    await eventArgs.SendSuccessAsync("Guild blacklisted.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }

                break;
            case "infractions":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var user = slashCommandInteraction.Entities.Users.Values.First();
                var infractions = await _moderationService.FetchInfractionsAsync(user.Id);
                if (!infractions.Any())
                {
                    await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                        .WithContent("That user does not have any infractions."));
                    break;
                }
                var userInfractionsEmbed = new LocalEmbed()
                    .WithColor(Color.Gold)
                    .WithTitle($"Infractions for {user.Tag}");
                foreach (var userInfraction in infractions)
                {
                    var moderator = Bot.GetGuild(userInfraction.GuildId).GetMember(userInfraction.ModeratorId);
                    userInfractionsEmbed.AddField($"{userInfraction.Type.ToString().ToUpper()}({userInfraction.Id}) - Created On {userInfraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {userInfraction.Reason}");
                }

                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithEmbeds(userInfractionsEmbed));
                break;
            case "infraction":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var infractionLookupId = slashCommandInteraction.Options.GetValueOrDefault("id")?.Value as string;
                if (!Guid.TryParse(infractionLookupId, out var guidInfractionLookupId))
                {
                    await eventArgs.SendEphmeralErrorAsync($"The ID provided is not a valid infration ID.");
                    break;
                }

                var infraction = await _moderationService.FetchInfractionAsync(guidInfractionLookupId);
                if (infraction == null)
                {
                    await eventArgs.SendEphmeralErrorAsync("That infraction ID does not exist.");
                    break;
                }

                var subject = await Bot.FetchUserAsync(infraction.SubjectId);
                var mod = guild.GetMember(infraction.ModeratorId);
                var embed = new LocalEmbed()
                    .AddField($"{infraction.Type.ToString().ToUpper()} - ({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {mod.Tag}", $"Reason: {infraction.Reason}")
                    .WithColor(Color.Gold)
                    .WithTitle($"Infractions for {subject.Tag}");
                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithEmbeds(embed));
                break;
            case "infraction-update":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var updateInfractionId = slashCommandInteraction.Options.GetValueOrDefault("id")?.Value as string;
                if (!Guid.TryParse(updateInfractionId, out var guidInfractionUpdateId))
                {
                    await eventArgs.SendEphmeralErrorAsync($"The ID provided is not a valid infration ID.");
                    break;
                }

                var updateInfractionReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;
                try
                {
                    await _moderationService.UpdateInfractionAsync(guidInfractionUpdateId, guild.Id, updateInfractionReason);
                    await eventArgs.SendSuccessAsync("Reason updated.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }

                break;
            case "infraction-delete":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }
                var deleteInfractionId = slashCommandInteraction.Options.GetValueOrDefault("id")?.Value as string;
                if (!Guid.TryParse(deleteInfractionId, out var guidDeleteInfractionId))
                {
                    await eventArgs.SendEphmeralErrorAsync($"The ID provided is not a valid infration ID.");
                    break;
                }

                var deleteInfractionReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;
                try
                {
                    await _moderationService.RemoveInfractionAsync(guidDeleteInfractionId, guild.Id, deleteInfractionReason);
                    await eventArgs.SendSuccessAsync("Infraction deleted.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }

                break;
        }
    }
}