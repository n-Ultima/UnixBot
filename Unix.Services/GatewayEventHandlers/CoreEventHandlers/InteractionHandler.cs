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
using Microsoft.Extensions.Logging;
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
    private readonly IReactionRoleService _reactionRoleService;
    private readonly ILogger<InteractionHandler> _logger;
    private readonly UnixConfiguration UnixConfig = new();
    public InteractionHandler(IServiceProvider serviceProvider, IOwnerService ownerService, IGuildService guildService, IModerationService moderationService, IReminderService reminderService, ITagService tagService, IReactionRoleService reactionRoleService, ILogger<InteractionHandler> logger) : base(serviceProvider)
    {
        _ownerService = ownerService;
        _guildService = guildService;
        _moderationService = moderationService;
        _reminderService = reminderService;
        _tagService = tagService;
        _reactionRoleService = reactionRoleService;
        _logger = logger;
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
                    if (userInfraction.IsRescinded)
                    {
                        userInfractionsEmbed.AddField($"(RESCINDED) {userInfraction.Type.ToString().ToUpper()}({userInfraction.Id}) - Created On {userInfraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {userInfraction.Reason}");
                    }
                    else
                    {
                        userInfractionsEmbed.AddField($"{userInfraction.Type.ToString().ToUpper()}({userInfraction.Id}) - Created On {userInfraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {userInfraction.Reason}");
                    }
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
                    .WithColor(Color.Gold)
                    .WithTitle($"Infractions for {subject.Tag}");
                if (infraction.IsRescinded)
                {
                    embed.AddField($"(RESCINDED) {infraction.Type.ToString().ToUpper()} - ({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {mod.Tag}", $"Reason: {infraction.Reason}");
                }
                else
                {
                    embed.AddField($"{infraction.Type.ToString().ToUpper()} - ({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {mod.Tag}", $"Reason: {infraction.Reason}");
                }

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
            case "infraction-rescind":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var infractionIdString = slashCommandInteraction.Options.GetValueOrDefault("id")?.Value as string;
                var rescindReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;
                if (!Guid.TryParse(infractionIdString, out var guidRescindInfractionId))
                {
                    await eventArgs.SendEphmeralErrorAsync("The ID provided is not a valid infraction ID.");
                    break;
                }

                try
                {
                    await _moderationService.RescindInfractionAsync(guidRescindInfractionId, guild.Id, eventArgs.Member.Id, rescindReason);
                    await eventArgs.SendSuccessAsync("Infraction rescinded.");
                    break;
                }
                catch (Exception e)
                {
                    await eventArgs.SendEphmeralErrorAsync(e.Message);
                    break;
                }
            case "infraction-unrescind":
                if (!eventArgs.Member.IsModerator())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Moderator);
                    break;
                }

                var unrescindIdString = slashCommandInteraction.Options.GetValueOrDefault("id")?.Value as string;
                var unrescindReason = slashCommandInteraction.Options.GetValueOrDefault("reason")?.Value as string;
                if (!Guid.TryParse(unrescindIdString, out var guidUnRescindInfractionId))
                {
                    await eventArgs.SendEphmeralErrorAsync("The ID provided is not a valid infraction ID.");
                    break;
                }

                try
                {
                    await _moderationService.UnRescindInfractionAsync(guidUnRescindInfractionId, guild.Id, eventArgs.Member.Id, unrescindReason);
                    await eventArgs.SendSuccessAsync("Infraction has been un-rescinded successfully.");
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

                if (muteTimeSpanDuration > TimeSpan.FromDays(28))
                {
                    await eventArgs.SendEphmeralErrorAsync("The duration of the mute can't be longer than 28 days.");
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
                        await _moderationService.LogInfractionDeletionAsync(new Infraction() { GuildId = guild.Id, Type = InfractionType.Mute }, eventArgs.Member, gUnmuteUser, false, unmuteReason);
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
                        await _moderationService.LogInfractionDeletionAsync(new Infraction { GuildId = guild.Id, Type = InfractionType.Ban }, eventArgs.Member, banNoInf.User, false, unbanReason);
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
                        .AddField("Joined", $"{Markdown.Timestamp(guildMember.JoinedAt.Value)}({(DateTimeOffset.UtcNow - guildMember.JoinedAt.Value).Humanize()} ago.)")
                        .AddField("Created", $"{Markdown.Timestamp(guildMember.CreatedAt())}({(DateTimeOffset.UtcNow - guildMember.CreatedAt()).Humanize()} ago.)")
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
    }
}