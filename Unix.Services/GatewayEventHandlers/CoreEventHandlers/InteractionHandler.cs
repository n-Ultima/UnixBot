using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Extensions.Interactivity;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Serilog;
using Unix.Common;
using Unix.Data.Models.Core;
using Unix.Data.Models.Moderation;
using Unix.Services.Core;
using Unix.Services.Core.Abstractions;
using Unix.Services.Extensions;
using Unix.Services.Parsers;

namespace Unix.Services.GatewayEventHandlers.CoreEventHandlers;

public class InteractionHandler : UnixService
{
    private readonly IOwnerService _ownerService;
    private readonly IGuildService _guildService;
    private readonly IModerationService _moderationService;
    private readonly IReminderService _reminderService;
    private readonly ITagService _tagService;
    private readonly UnixConfiguration UnixConfig = new();
    public InteractionHandler(IServiceProvider serviceProvider, IOwnerService ownerService, IGuildService guildService, IModerationService moderationService, IReminderService reminderService, ITagService tagService) : base(serviceProvider)
    {
        _ownerService = ownerService;
        _guildService = guildService;
        _moderationService = moderationService;
        _reminderService = reminderService;
        _tagService = tagService;
    }

    protected override async ValueTask OnInteractionReceived(InteractionReceivedEventArgs eventArgs)
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
            if (UnixConfig.PrivelegedMode)
            {
                await eventArgs.SendEphmeralErrorAsync($"You must request access with Unix before use. Please see http://www.ultima.one/unix");
                return;
            }
        }

        var guild = Bot.GetGuild(eventArgs.GuildId.Value);
        if (!eventArgs.Member.CanUseCommands())
        {
            await eventArgs.SendEphmeralErrorAsync($"Missing permissions.");
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

                var isEnabled = (bool)enabled.Value;
                try
                {
                    await _guildService.ModifyGuildAutomodAsync(eventArgs.GuildId.Value, isEnabled);
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

                var actualSpamAmount = (int)amount.Value;
                await _guildService.ModifyGuildSpamThresholdAsync(eventArgs.GuildId.Value, actualSpamAmount);
                await eventArgs.SendSuccessAsync($"If a user sends more than `{actualSpamAmount}` in `3` seconds, they will be warned.");
                break;
            case "configure-phisherman":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var apiKey = slashCommandInteraction.Options.GetValueOrDefault("key")?.Value as string;
                try
                {
                    await _guildService.ModifyGuildPhishermanApiKeyAsync(guild.Id, apiKey);
                    await eventArgs.SendSuccessAsync($"Phisherman API Key set!");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "configure-requiredrole":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var reqRole = slashCommandInteraction.Options.GetValueOrDefault("id")?.Value as string;
                if (!Snowflake.TryParse(reqRole, out var reqRoleId))
                {
                    await eventArgs.SendEphmeralErrorAsync($"Invalid ID provided.");
                    break;
                }

                try
                {
                    await _guildService.ModifyGuildRequiredRoleAsync(guild.Id, reqRoleId);
                    if (guild.Id == reqRoleId)
                    {
                        await eventArgs.SendSuccessAsync($"No users are blocked from using commands in this server(except moderation and administration commands).");
                        break;
                    }

                    var role = guild.Roles.GetValueOrDefault(reqRoleId);
                    if (role == null)
                    {
                        await eventArgs.SendEphmeralErrorAsync($"Invalid role ID provided.");
                        break;
                    }

                    await eventArgs.SendSuccessAsync($"Unix will now ignore commands from users without the **{role.Name}** role.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "configure-miscellaneouslog":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var miscChannel = slashCommandInteraction.Entities.Channels.First();
                await _guildService.ModifyGuildMiscellaneousLogChannelIdAsync(guild.Id, miscChannel.Value.Id);
                await eventArgs.SendSuccessAsync($"Miscellaneous log events will now be logged to {Mention.Channel(miscChannel.Value.Id)}");
                break;
            case "add-banned-term":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var bannedTermOption = slashCommandInteraction.Options.GetValueOrDefault("term")?.Value as string;
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

                var removedBannedTermOption = slashCommandInteraction.Options.GetValueOrDefault("term")?.Value as string;
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
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
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
                    await _guildService.RemoveWhitelistedInviteAsync(eventArgs.GuildId.Value, realBlacklistSnowflake);
                    await eventArgs.SendSuccessAsync($"Invites pointing towards **{guild.Name}** will now be deleted.");
                    break;
                }
                catch (Exception e)
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
                var modLogString = slashCommandInteraction.Options.GetValueOrDefault("modlog-channel-id")?.Value as string;
                var messageLogString = slashCommandInteraction.Options.GetValueOrDefault("messagelog-channel-id")?.Value as string;
                var miscellaneousLogString = slashCommandInteraction.Options.GetValueOrDefault("miscellaneous-channel-id")?.Value as string;
                var modRoleString = slashCommandInteraction.Options.GetValueOrDefault("moderator-role-id")?.Value as string;
                var adminRoleString = slashCommandInteraction.Options.GetValueOrDefault("administrator-role-id")?.Value as string;
                var isAutomodEnabled = slashCommandInteraction.Options.TryGetValue("automod-enabled", out var sCommandInteraction);
                var autoModEnabled = (bool)sCommandInteraction.Value;
                if (!Snowflake.TryParse(guildIdString, out var realGuildId))
                {
                    await eventArgs.SendEphmeralErrorAsync($"Invalid snowflake provided for guild ID.");
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

                if (!Snowflake.TryParse(miscellaneousLogString, out var realMiscellaneousLog))
                {
                    await eventArgs.SendEphmeralErrorAsync("Invalid snowflake provided for miscellaneous log ID.");
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
                try
                {
                    await _ownerService.ConfigureGuildAsync(realGuildId, realModLog, realMessageLog, realMiscellaneousLog, realModRole, realAdminRole, autoModEnabled);
                    await eventArgs.SendSuccessAsync($"Successfully configured!");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
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
            case "infractions":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var user = slashCommandInteraction.Entities.Users.Values.First();
                var infractions = await _moderationService.FetchInfractionsAsync(guild.Id, user.Id);
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

                var infraction = await _moderationService.FetchInfractionAsync(guidInfractionLookupId, guild.Id);
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
                    await _moderationService.RemoveInfractionAsync(guidDeleteInfractionId, guild.Id, eventArgs.Member.Id, false, deleteInfractionReason);
                    await eventArgs.SendSuccessAsync("Infraction deleted.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "ban":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                TimeSpan? btS = null;
                var banUser = slashCommandInteraction.Entities.Users.Values.First();
                var banReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;
                var duration = slashCommandInteraction.Options.GetValueOrDefault("duration")?.Value as string;
                if (duration != null)
                {
                    if (!TimeSpanParser.TryParseTimeSpan(duration, out var banDuration))
                    {
                        await eventArgs.SendEphmeralErrorAsync("The duration provided is not valid.");
                        break;
                    }

                    btS = banDuration;
                }

                try
                {
                    await _moderationService.CreateInfractionAsync(guild.Id, eventArgs.Member.Id, banUser.Id, InfractionType.Ban, banReason, false, btS);
                    await eventArgs.SendSuccessAsync($"Banned **{banUser.Tag}** | `{banReason}`");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "mute":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                TimeSpan? mtS = null;
                var muteUser = slashCommandInteraction.Entities.Users.Values.First();
                var muteReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;
                var muteDuration = slashCommandInteraction.Options.GetValueOrDefault("duration")?.Value as string;
                if (muteUser is not IMember)
                {
                    await eventArgs.SendEphmeralErrorAsync("Member not found.");
                    break;
                }

                if (!TimeSpanParser.TryParseTimeSpan(muteDuration, out var muteTimeSpanDuration))
                {
                    await eventArgs.SendEphmeralErrorAsync("The duration provided is not valid.");
                    break;
                }

                mtS = muteTimeSpanDuration;
                try
                {
                    await _moderationService.CreateInfractionAsync(guild.Id, eventArgs.Member.Id, muteUser.Id, InfractionType.Mute, muteReason, false, mtS);
                    await eventArgs.SendSuccessAsync($"Muted **{muteUser.Tag}** | `{muteReason}`");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "note":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var noteUser = slashCommandInteraction.Entities.Users.Values.First();
                var noteReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;
                if (noteUser is not IMember)
                {
                    await eventArgs.SendEphmeralErrorAsync("Member not found.");
                    break;
                }

                try
                {
                    await _moderationService.CreateInfractionAsync(guild.Id, eventArgs.Member.Id, noteUser.Id, InfractionType.Note, noteReason, false, null);
                    await eventArgs.SendSuccessAsync($"Note recorded for **{noteUser.Tag}** | `{noteReason}`");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "warn":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var warnUser = slashCommandInteraction.Entities.Users.Values.First();
                var warnReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;
                if (warnUser is not IMember)
                {
                    await eventArgs.SendEphmeralErrorAsync("Member not found.");
                    break;
                }

                try
                {
                    await _moderationService.CreateInfractionAsync(guild.Id, eventArgs.Member.Id, warnUser.Id, InfractionType.Warn, warnReason, false, null);
                    await eventArgs.SendSuccessAsync($"Warned **{warnUser.Tag}** | `{warnReason}`");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "purge":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var purgeCount = Convert.ToInt32(slashCommandInteraction.Options.GetValueOrDefault("count")?.Value);
                var purgeUser = slashCommandInteraction.Entities.Users.Values.FirstOrDefault();
                if (purgeCount > 100)
                {
                    await eventArgs.SendEphmeralErrorAsync("You can't purge more than 100 messages at once.");
                    break;
                }

                IEnumerable<Snowflake> delMessages = new List<Snowflake>();
                if (purgeUser != null)
                {
                    delMessages = (await Bot.FetchMessagesAsync(eventArgs.ChannelId))
                        .Where(x => x.Author.Id == purgeUser.Id)
                        .Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14)
                        .Select(x => x.Id)
                        .Take(purgeCount);
                }
                else
                {
                    delMessages = (await Bot.FetchMessagesAsync(eventArgs.ChannelId))
                        .Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14)
                        .Select(x => x.Id)
                        .Take(purgeCount);
                }

                await Bot.DeleteMessagesAsync(eventArgs.ChannelId, delMessages);
                if (purgeUser != null)
                {
                    await eventArgs.SendSuccessAsync($"Purged **{delMessages.Count()}** sent by **{purgeUser.Tag}**");
                    break;
                }

                await eventArgs.SendSuccessAsync($"Purged **{delMessages.Count()}**");
                break;
            case "unmute":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }
                var unmuteUser = slashCommandInteraction.Entities.Users.Values.First();
                var unmuteReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;

                if (unmuteUser is not IMember gUnmuteUser)
                {
                    await eventArgs.SendEphmeralErrorAsync("Member not found.");
                    break;
                }

                var unmuteUserInfractions = await _moderationService.FetchInfractionsAsync(guild.Id, unmuteUser.Id);
                var mute = unmuteUserInfractions
                    .Where(x => x.Type == InfractionType.Mute)
                    .Where(x => x.GuildId == guild.Id)
                    .SingleOrDefault();
                if (mute == null)
                {
                    if (!gUnmuteUser.TimedOutUntil.HasValue || gUnmuteUser.TimedOutUntil < DateTimeOffset.UtcNow)
                    {
                        await eventArgs.SendEphmeralErrorAsync("The user provided is not currently timed out.");
                        break;
                    }
                    if (gUnmuteUser.TimedOutUntil > DateTimeOffset.UtcNow)
                    {
                        await _moderationService.LogInfractionDeletionAsync(new Infraction()
                        {
                            GuildId = guild.Id,
                            Type = InfractionType.Mute
                        }, eventArgs.Member, gUnmuteUser, false, unmuteReason);
                        await eventArgs.SendSuccessAsync($"Removed the timeout for **{gUnmuteUser.Tag}** | `{unmuteReason}`");
                        break;
                    }
                }

                try
                {
                    await _moderationService.RemoveInfractionAsync(mute.Id, guild.Id, eventArgs.Member.Id, false, unmuteReason);
                    await eventArgs.SendSuccessAsync($"Removed the time out for **{unmuteUser.Tag}** | `{unmuteReason}`");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "unban":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var unbanUser = slashCommandInteraction.Entities.Users.Values.First();
                var unbanReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;
                var unbanUserInfractions = await _moderationService.FetchInfractionsAsync(guild.Id, unbanUser.Id);
                var banInfraction = unbanUserInfractions
                    .Where(x => x.Type == InfractionType.Ban)
                    .SingleOrDefault();
                if (banInfraction == null)
                {
                    var banNoInf = await guild.FetchBanAsync(unbanUser.Id);
                    if (banNoInf != null)
                    {
                        await _moderationService.LogInfractionDeletionAsync(new Infraction
                        {
                            GuildId = guild.Id,
                            Type = InfractionType.Ban
                        }, eventArgs.Member, banNoInf.User, false, unbanReason);
                        await eventArgs.SendSuccessAsync($"Unbanned **{banNoInf.User.Tag}** | `{unbanReason}`");
                        break;
                    }
                    else
                    {
                        await eventArgs.SendEphmeralErrorAsync("That user is not currently banned.");
                    }
                }

                try
                {
                    await _moderationService.RemoveInfractionAsync(banInfraction.Id, guild.Id, eventArgs.Member.Id, false, unbanReason);
                    await eventArgs.SendSuccessAsync($"Unbanned **{unbanUser.Tag}** | `{unbanReason}`");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }

            case "kick":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var kickUser = slashCommandInteraction.Entities.Users.Values.First();
                var kickReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;
                if (kickUser is not IMember)
                {
                    await eventArgs.SendEphmeralErrorAsync("Member not found.");
                    break;
                }

                try
                {
                    await _moderationService.CreateInfractionAsync(guild.Id, eventArgs.Member.Id, kickUser.Id, InfractionType.Kick, kickReason, false, null);
                    await eventArgs.SendSuccessAsync($"Kicked **{kickUser.Tag}** | `{kickReason}`");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "role-add":
                var roleToAdd = slashCommandInteraction.Entities.Roles.Values.First();
                if (!guildConfig.SelfAssignableRoles.Contains(roleToAdd.Id.RawValue))
                {
                    await eventArgs.SendEphmeralErrorAsync("Unknown role.");
                    break;
                }

                if (eventArgs.Member.RoleIds.Contains(roleToAdd.Id))
                {
                    await eventArgs.SendEphmeralErrorAsync("You already have that role, so I can't give it to you.");
                    break;
                }

                await Bot.GrantRoleAsync(guild.Id, eventArgs.Member.Id, roleToAdd.Id);
                await eventArgs.SendSuccessAsync($"Granted you the **{roleToAdd.Name}** role.");
                break;
            case "role-remove":
                var roleToRemove = slashCommandInteraction.Entities.Roles.Values.First();
                if (!guildConfig.SelfAssignableRoles.Contains(roleToRemove.Id.RawValue))
                {
                    await eventArgs.SendEphmeralErrorAsync("Unknown role.");
                    break;
                }

                if (!eventArgs.Member.RoleIds.Contains(roleToRemove.Id))
                {
                    await eventArgs.SendEphmeralErrorAsync("You don't have that role, so I can't remove it.");
                    break;
                }

                await Bot.RevokeRoleAsync(guild.Id, eventArgs.Member.Id, roleToRemove.Id);
                await eventArgs.SendSuccessAsync($"Removed the **{roleToRemove.Name}** role from you.");
                break;
            case "configure-role-add":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var configRoleToAdd = slashCommandInteraction.Entities.Roles.Values.First();
                var botUser = guild.GetMember(Bot.CurrentUser.Id);
                if (configRoleToAdd.Position > botUser.GetHierarchy())
                {
                    await eventArgs.SendEphmeralErrorAsync("The bot must have a higher role position than the role provided.");
                    break;
                }

                try
                {
                    await _guildService.AddSelfAssignableRoleAsync(guild.Id, configRoleToAdd.Id);
                    await eventArgs.SendSuccessAsync($"Users can now add this role to themselves with the /role-add command.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "configure-role-remove":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var configRoleToRemove = slashCommandInteraction.Entities.Roles.Values.First();
                var bUser = guild.GetMember(Bot.CurrentUser.Id);
                if (configRoleToRemove.Position > bUser.GetHierarchy())
                {
                    await eventArgs.SendEphmeralErrorAsync("The bot must have a higher role position than the role provided.");
                    break;
                }

                try
                {
                    await _guildService.RemoveSelfAssignableRoleAsync(guild.Id, configRoleToRemove.Id);
                    await eventArgs.SendSuccessAsync("Users can no longer add this role to themselves.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "role":
                List<string> roles = new();
                foreach (var roleId in guildConfig.SelfAssignableRoles)
                {
                    var name = guild.Roles.Where(x => x.Key == roleId).Select(x => x.Value).SingleOrDefault();
                    if (name == null)
                    {
                        Log.Logger.Information("The role {id} was not found in the guild(manual deletion?). Removing it from the list of assignable roles.");
                        await _guildService.RemoveSelfAssignableRoleAsync(guild.Id, name.Id);
                        continue;
                    }

                    roles.Add(name.Name);
                }

                var roleHelpEmbed = new LocalEmbed()
                    .WithAuthor(guild.Name, guild.GetIconUrl())
                    .WithTitle("How do I get roles?")
                    .WithColor(Color.Aqua)
                    .WithDescription(
                        $"To give yourself a role, use the `/role-add <roleName>` where **roleName** is whatever role you want.\nTo remove a role, use the `/role-remove <roleName>` replacing **roleName** with the role you want to remove.\n")
                    .AddField("Roles available to you:", !roles.Any()
                        ? "None"
                        : roles.Humanize());
                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithIsEphemeral()
                    .WithEmbeds(roleHelpEmbed));
                break;
            case "remind":
                var remindTs = slashCommandInteraction.Options.GetValueOrDefault("duration")?.Value as string;
                var remindMessage = slashCommandInteraction.Options.GetValueOrDefault("message")?.Value as string;
                if (!TimeSpanParser.TryParseTimeSpan(remindTs, out var reminderTimeSpan))
                {
                    await eventArgs.SendEphmeralErrorAsync("Invalid time span provided.");
                    break;
                }

                try
                {
                    await _reminderService.CreateReminderAsync(guild.Id, eventArgs.ChannelId, eventArgs.Member.Id, reminderTimeSpan, remindMessage);
                    await eventArgs.SendSuccessAsync($"Reminder set for {reminderTimeSpan.Humanize(precision: 10)}");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "reminder-delete":
                var reminderId = Convert.ToInt64(slashCommandInteraction.Options.GetValueOrDefault("id")?.Value);
                var reminderToDelete = await _reminderService.FetchReminderAsync(reminderId);
                if (reminderToDelete == null)
                {
                    await eventArgs.SendEphmeralErrorAsync("A reminder with that ID doesn't exist.");
                    break;
                }

                if (reminderToDelete.UserId != eventArgs.Member.Id)
                {
                    if (!eventArgs.Member.IsModerator() || !eventArgs.Member.IsAdmin())
                    {
                        await eventArgs.SendEphmeralErrorAsync("You must either own this reminder or be a moderator or administrator to delete it.");
                        break;
                    }
                }

                try
                {
                    await _reminderService.DeleteReminderAsync(reminderId);
                    await eventArgs.SendSuccessAsync($"Reminder deleted.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "reminders":
                var currentReminders = await _reminderService.FetchRemindersForUserAsync(guild.Id, eventArgs.Member.Id);
                if (!currentReminders.Any())
                {
                    await eventArgs.SendEphmeralErrorAsync($"No reminders found.");
                    break;
                }

                var currentRemindersEmbed = new LocalEmbed()
                    .WithColor(System.Drawing.Color.Gold);
                foreach (var reminder in currentReminders.OrderBy(x => x.ExecutionTime))
                {
                    currentRemindersEmbed.AddField($"Reminder {reminder.Id}", $"{Markdown.Timestamp(reminder.ExecutionTime)} - `{reminder.Value}`");
                }

                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithEmbeds(currentRemindersEmbed));
                break;
            case "tags":
                var tags = await _tagService.FetchTagsAsync(guild.Id);
                if (!tags.Any())
                {
                    await eventArgs.SendEphmeralErrorAsync("No tags were found.");
                    break;
                }

                var arrTags = tags.Select(x => x.Name).ToArray();
                var arrPageProvider = new ArrayPageProvider<string>(arrTags, itemsPerPage: arrTags.Length > 10
                    ? 10
                    : arrTags.Length);
                var pgView = new PagedTagView(arrPageProvider);
                await pgView.UpdateAsync();
                var interactionPgView = pgView.ToLocalMessage().ToLocalInteractionResponse();
                await eventArgs.Interaction.Response().SendMessageAsync(interactionPgView);
                var message = await eventArgs.Interaction.Followup().FetchResponseAsync();
                var menu = new DefaultMenu(pgView, message.Id);
                await Bot.StartMenuAsync(eventArgs.ChannelId, menu);
                break;
            case "tag":
                var tagOption = slashCommandInteraction.Options.GetValueOrDefault("name")?.Value as string;
                var tagToSend = await _tagService.FetchTagAsync(guild.Id, tagOption);
                if (tagToSend == null)
                {
                    await eventArgs.SendEphmeralErrorAsync("That tag does not exist.");
                    break;
                }

                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithContent(tagToSend.Content));
                break;
            case "tag-create":
                var newTagName = slashCommandInteraction.Options.GetValueOrDefault("name")?.Value as string;
                var newTagContent = slashCommandInteraction.Options.GetValueOrDefault("content")?.Value as string;
                try
                {
                    await _tagService.CreateTagAsync(guild.Id, eventArgs.Member.Id, newTagName, newTagContent);
                    await eventArgs.SendSuccessAsync($"Tag **{newTagName}** created.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "tag-edit":
                var tagEditOption = slashCommandInteraction.Options.GetValueOrDefault("name")?.Value as string;
                var tagContentOption = slashCommandInteraction.Options.GetValueOrDefault("content")?.Value as string;
                try
                {
                    await _tagService.EditTagContentAsync(guild.Id, eventArgs.Member.Id, tagEditOption, tagContentOption);
                    await eventArgs.SendSuccessAsync("Tag contenet modified.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "tag-transfer":
                var tagTransferOption = slashCommandInteraction.Options.GetValueOrDefault("name")?.Value as string;
                var newOwner = slashCommandInteraction.Entities.Users.Select(x => x.Value).First();
                if (newOwner is not IMember)
                {
                    await eventArgs.SendEphmeralErrorAsync("Member not found.");
                    break;
                }

                try
                {
                    await _tagService.EditTagOwnershipAsync(guild.Id, eventArgs.Member.Id, tagTransferOption, newOwner.Id);
                    await eventArgs.SendSuccessAsync($"Tag successfully transfered to **{newOwner.Tag}**");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "tag-delete":
                var tagDeleteOption = slashCommandInteraction.Options.GetValueOrDefault("name")?.Value as string;
                try
                {
                    await _tagService.DeleteTagAsync(guild.Id, eventArgs.Member.Id, tagDeleteOption);
                    await eventArgs.SendSuccessAsync("Tag deleted.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "botinfo":
                var botInfoEmbed = new LocalEmbed()
                    .WithTitle("Unix Info")
                    .WithAuthor(Bot.CurrentUser)
                    .WithColor(Color.Purple)
                    .WithDescription("Unix is a multi feature Discord bot that boasts reliability, efficiency, and simplicity. You can vote for the bot on [top.gg](https://top.gg/bot/817577290057383947).")
                    .AddField("Developer(s)", "Ultima#2000", true)
                    .AddField("Contributor(s)", "Voxel#8113", true)
                    .AddField("Github", "https://www.github.com/n-Ultima/UnixBot")
                    .AddField("Library", "Disqord", true)
                    .AddField("Language", "C#", true)
                    .AddField("Support Server", "http://www.ultima.one/unix", true);
                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithEmbeds(botInfoEmbed));
                break;
            case "info":
                var userOption = slashCommandInteraction.Entities.Users.Values.FirstOrDefault();
                if (userOption is IMember guildMember)
                {
                    var guildMemberEmbed = new LocalEmbed()
                        .WithTitle(guildMember.Tag)
                        .WithAuthor(guildMember)
                        .WithThumbnailUrl(guildMember.GetAvatarUrl() ?? guildMember.GetDefaultAvatarUrl())
                        .AddField("ID", guildMember.Id)
                        .AddField("Joined", Markdown.Timestamp(guildMember.JoinedAt.Value))
                        .AddField("Created", Markdown.Timestamp(guildMember.CreatedAt()))
                        .AddField("Roles", guildMember.GetRoles().Select(x => x.Value.Name).Humanize())
                        .AddField("Hierarchy", guild.OwnerId == guildMember.Id ? "Guild Owner" : guildMember.GetHierarchy())
                        .WithColor(Color.Gold);
                    await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                        .WithEmbeds(guildMemberEmbed));
                    break;
                }

                if (userOption == null)
                {
                    var requestor = slashCommandInteraction.Author as IMember;
                    var requestorEmbed = new LocalEmbed()
                        .WithTitle(requestor.Tag)
                        .WithAuthor(requestor)
                        .WithThumbnailUrl(requestor.GetAvatarUrl() ?? requestor.GetDefaultAvatarUrl())
                        .AddField("ID", requestor.Id)
                        .AddField("Joined", Markdown.Timestamp(requestor.JoinedAt.Value))
                        .AddField("Created", Markdown.Timestamp(requestor.CreatedAt()))
                        .AddField("Roles", requestor.GetRoles().Select(x => x.Value.Name).Humanize())
                        .AddField("Hierarchy", guild.OwnerId == requestor.Id ? "Guild Owner" : requestor.GetHierarchy())
                        .WithColor(Color.Gold);
                    await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                        .WithEmbeds(requestorEmbed));
                    break;
                }
                var userEmbed = new LocalEmbed()
                    .WithTitle(userOption.Tag)
                    .WithAuthor(userOption)
                    .WithThumbnailUrl(userOption.GetAvatarUrl() ?? userOption.GetDefaultAvatarUrl())
                    .AddField("ID", userOption.Id)
                    .AddField("Created", Markdown.Timestamp(userOption.CreatedAt()))
                    .WithColor(Color.Gold);
                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithEmbeds(userEmbed));
                break;
        }
        Log.Logger.Information("Slash command {sName}(executed by {userName}) was handled.", slashCommandInteraction.CommandName, eventArgs.Member.Tag);
    }
}