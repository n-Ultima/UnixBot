using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Components;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Extensions.Interactivity.Menus.Prompt;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Qmmands;
using Qommon;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;
using Unix.Services.Extensions;

namespace Unix.Modules;

[SlashGroup("tag")]
public class TagModule : UnixModuleBase
{
    private readonly ITagService _tagService;
    public TagModule(IGuildService guildConfigurationService, ITagService tagService) : base(guildConfigurationService)
    {
        _tagService = tagService;
    }

    [SlashCommand("list")]
    [Description("Lists all tags in this server.")]
    public async Task ListTagsAsync()
    {
        var tags = await _tagService.FetchTagsAsync(Context.GuildId);
        if (!tags.Any())
        {
            EphmeralFailure("There are no tags in this server.");
            return;
        }

        var tagNames = tags.Select(x => x.Name).ToArray();
        var tagPageProvider = new ArrayPageProvider<string>(tagNames, itemsPerPage: tagNames.Length > 10
            ? 10
            : tagNames.Length);
        var tagView = new PagedTagView(tagPageProvider);
        await tagView.UpdateAsync();
        // make this bad boy an interaction
        var message = new LocalInteractionMessageResponse();
        tagView.FormatLocalMessage(message);
        await Context.Interaction.Response().SendMessageAsync(message);
        var fetchMessage = await Context.Interaction.Followup().FetchResponseAsync();
        var menu = new DefaultTextMenu(tagView, fetchMessage.Id);
        await Bot.StartMenuAsync(Context.ChannelId, menu);
    }
    [SlashCommand("create")]
    [Description("Calls a modal for creating a tag.")]
    public async Task CreateTagAsync()
    {
        await Context.Interaction.Response().SendModalAsync(new LocalInteractionModalResponse()
            .WithTitle("Tag Creation")
            .WithCustomId("tag_create")
            .WithComponents(new []
            {
                new LocalRowComponent()
                {
                    Components = new List<LocalComponent>()
                    {
                        new LocalTextInputComponent()
                        {
                            CustomId = "tag_create_name",
                            IsRequired = true,
                            Label = "Name",
                            MinimumInputLength = 1,
                            Style = TextInputComponentStyle.Short
                        }
                    }
                },
                new LocalRowComponent()
                {
                    Components = new List<LocalComponent>()
                    {
                        new LocalTextInputComponent()
                        {
                            CustomId = "tag_create_content",
                            IsRequired = true,
                            Label = "Content",
                            MinimumInputLength = 1,
                            Style = TextInputComponentStyle.Paragraph
                        }
                    }
                }
            })
        );
    }

    [SlashCommand("edit")]
    [Description("Returns a modal to update a tag.")]
    public async Task EditTagAsync()
    {
        await Context.Interaction.Response().SendModalAsync(new LocalInteractionModalResponse()
            .WithTitle("Edit Tag")
            .WithCustomId("tag_edit")
            .WithComponents(new []
            {
                new LocalRowComponent()
                {
                    Components = new List<LocalComponent>()
                    {
                        new LocalTextInputComponent()
                        {
                            CustomId = "edit_tag_name",
                            IsRequired = true,
                            Label = "Name",
                            MinimumInputLength = 1,
                            Style = TextInputComponentStyle.Short
                        }
                    }
                },
                new LocalRowComponent()
                {
                    Components = new List<LocalComponent>()
                    {
                        new LocalTextInputComponent()
                        {
                            CustomId = "edit_tag_content",
                            IsRequired = true,
                            Label = "New Content",
                            MinimumInputLength = 1,
                            Style = TextInputComponentStyle.Paragraph
                        }
                    }
                }
            })
        );
    }

    [SlashCommand("transfer")]
    [Description("Transfer a tag to another user.")]
    public async Task<IResult> TransferTagAsync(IMember newOwner, string tagName)
    {
        var tag = await _tagService.FetchTagAsync(Context.GuildId, tagName);
        if (tag is null)
        {
            return EphmeralFailure("That tag does not exist.");
        }

        if (tag.OwnerId == newOwner.Id)
        {
            return EphmeralFailure("You can't transfer a tag that you already own to yourself.");
        }

        if (Context.AuthorId != tag.OwnerId || !Context.Author.IsModerator())
        {
            return EphmeralFailure("You must either be a moderator or own this tag to perform that action.");
        }

        try
        {
            await _tagService.EditTagOwnershipAsync(Context.GuildId, Context.AuthorId, tagName, newOwner.Id);
            return Success($"The tag **{tag.Name}** is now owned by **{newOwner.Tag}**");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }
    [SlashCommand("delete")]
    [Description("Deletes a tag.")]
    public async Task<IResult> DeleteTagAsync(string tagName)
    {
        var tag = await _tagService.FetchTagAsync(Context.GuildId, tagName);
        if (tag is null)
        {
            return EphmeralFailure("The tag provided does not exist.");
        }

        if (Context.AuthorId != tag.OwnerId || !Context.Author.IsModerator())
        {
            return EphmeralFailure("You must either be a moderator or own this tag to perform that action.");
        }

        try
        {
            await _tagService.DeleteTagAsync(Context.GuildId, Context.AuthorId, tag.Name);
            return Success($"Tag **{tag.Name}** deleted.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }
}

public class TagModalModule : DiscordComponentGuildModuleBase
{
    private readonly IGuildService _guildConfigurationService;
    private readonly ITagService _tagService;

    public TagModalModule(IGuildService guildConfigurationService, ITagService tagService)
    {
        _guildConfigurationService = guildConfigurationService;
        _tagService = tagService;
    }
    [ModalCommand("tag_create")]
    public async Task<IResult> CreateTagAsync(string tag_create_name, string tag_create_content)
    {
        
        try
        {
            await _tagService.CreateTagAsync(Context.GuildId, Context.AuthorId, tag_create_name, tag_create_content);
            return Response($"<:unixok:884524202458222662> Tag **{tag_create_name}** created.");
        }
        catch (Exception e)
        {
            return Response(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent($"⚠ {e.Message}"));
        }
    }

    [ModalCommand("tag_edit")]
    public async Task<IResult> EditTagAsync(string edit_tag_name, string edit_tag_content)
    {
        var tag = await _tagService.FetchTagAsync(Context.GuildId, edit_tag_name);
        if (tag is null)
        {
            return Response(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent($"⚠ That tag does not exist."));
        }

        if (Context.AuthorId != tag.OwnerId || !Context.Author.IsModerator())
        {
            return Response(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent($"⚠ You must be either be a moderator or own this tag to perform that action."));
        }

        try
        {
            await _tagService.EditTagContentAsync(Context.GuildId, Context.AuthorId, edit_tag_name, edit_tag_content);
            return Response($"<:unixok:884524202458222662> Tag **{edit_tag_name}** updated.");
        }
        catch (Exception e)
        {
            return Response(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent($"⚠ {e.Message}"));
        }
    }
}
