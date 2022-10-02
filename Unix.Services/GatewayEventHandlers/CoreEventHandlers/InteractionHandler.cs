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
        }
    }
}