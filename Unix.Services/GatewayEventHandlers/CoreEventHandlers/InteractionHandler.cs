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