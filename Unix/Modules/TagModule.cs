using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Components;
using Disqord.Extensions.Interactivity.Menus;
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

    [SlashCommand("create")]
    [Description("Calls a modal for creating a tag.")]
    public async Task CreateTagAsync()
    {
        await Context.Interaction.Response().SendModalAsync(new LocalInteractionModalResponse()
            .WithTitle("Tag Creation")
            .WithCustomId("tag_create")
            .WithComponents(new []
            {
                new LocalTextInputComponent()
                {
                    CustomId = "tag_create_name",
                    IsRequired = true,
                    Label = "Name",
                    MinimumInputLength = 1,
                    Style = TextInputComponentStyle.Short
                },
                new LocalTextInputComponent()
                {
                    CustomId = "tag_create_content",
                    IsRequired = true,
                    Label = "Content",
                    MinimumInputLength = 1,
                    Style = TextInputComponentStyle.Paragraph
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
                new LocalTextInputComponent()
                {
                    CustomId = "edit_tag_name",
                    IsRequired = true,
                    Label = "Name",
                    MinimumInputLength = 1,
                    Style = TextInputComponentStyle.Short
                },
                new LocalTextInputComponent()
                {
                    CustomId = "edit_tag_content",
                    IsRequired = true,
                    Label = "New Content",
                    MinimumInputLength = 1,
                    Style = TextInputComponentStyle.Paragraph
                }
            })
        );
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
            return Response($"<:unixok:884524202458222662> Tag **{tag_create_content}** created.");
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

        if (Context.AuthorId != tag.OwnerId || Context.Author.IsModerator())
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
