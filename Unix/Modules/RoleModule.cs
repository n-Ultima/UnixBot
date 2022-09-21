using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.Extensions.Logging;
using Qmmands;
using Unix.Modules.Bases;
using Unix.Modules.Checks;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules;

public class RoleModule : UnixModuleBase
{
    private readonly IReactionRoleService _reactionRoleService;
    public RoleModule(IGuildService guildConfigurationService, IReactionRoleService reactionRoleService) : base(guildConfigurationService)
    {
        _reactionRoleService = reactionRoleService;
    }

    [SlashCommand("configure-role-add")]
    [RequireGuildAdministrator]
    [Description("Creates a self-assignable role that users can add to themselves. The role must already exist.")]
    public async Task<IResult> CreateRoleAsync(IRole role)
    {
        var botMember = Context.Author.GetGuild().GetMember(Bot.CurrentUser.Id);
        if (role.Position >= botMember.CalculateRoleHierarchy())
        {
            return EphmeralFailure("The role must be lower than Unix's role.");
        }
        try
        {
            await _guildConfigurationService.AddSelfAssignableRoleAsync(Context.GuildId, role.Id);
            return Success($"Users can now give themselves the **{role.Name}** role.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("configure-role-remove")]
    [RequireGuildAdministrator]
    [Description("Removes a self assignable role.")]
    public async Task<IResult> RemoveRoleAsync(IRole role)
    {
        try
        {
            await _guildConfigurationService.RemoveSelfAssignableRoleAsync(Context.GuildId, role.Id);
            return Success($"Users can no longer give themselves the **{role.Name}** role.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }
    
    [SlashCommand("add-autorole")]
    [RequireGuildAdministrator]
    [Description("Adds an autorole configuration.")]
    public async Task<IResult> CreateAutoRoleAsync(IRole role)
    {
        if (CurrentGuildConfiguration.AutoRoles.Count != 0 && CurrentGuildConfiguration.AutoRoles.Contains(role.Id.RawValue))
        {
            return EphmeralFailure("That role is already an auto role.");
        }

        try
        {
            await _guildConfigurationService.AddAutoRoleAsync(Context.GuildId, role.Id);
            return Success($"Upon joining, users will now be granted the **{role.Name}** role.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("remove-autorole")]
    [RequireGuildAdministrator]
    [Description("Removes an autorole configuration.")]
    public async Task<IResult> DeleteAutoRoleAsync(IRole role)
    {
        if (CurrentGuildConfiguration.AutoRoles.Count != 0 && !CurrentGuildConfiguration.AutoRoles.Contains(role.Id.RawValue))
        {
            return EphmeralFailure("That role is not currently an auto role.");
        }

        try
        {
            await _guildConfigurationService.RemoveAutoRoleAsync(Context.GuildId, role.Id);
            return Success($"Upon joining, users will no longer be granted the **{role.Name}** role.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("role")]
    [Description("Returns a list of all self assignable roles.")]
    public async Task<IResult> ListSelfAssignableRolesAsync(IRole role)
    {
        List<string> roles = new();
        foreach (var roleId in CurrentGuildConfiguration.SelfAssignableRoles)
        {
            var name = Context.Author.GetGuild().Roles.Where(x => x.Key == roleId).Select(x => x.Value).SingleOrDefault();
            if (name == null)
            {
                // The guild configuration has a role ID that doesn't currently exist in the guild.
                // Assuming the audit log handler didn't catch this, just remove the self assignable role.
                await _guildConfigurationService.RemoveSelfAssignableRoleAsync(Context.GuildId, name.Id);
                continue;
            }

            roles.Add(name.Name);
        }

        var roleHelpEmbed = new LocalEmbed()
            .WithAuthor(Context.Author.GetGuild().Name, Context.Author.GetGuild().GetIconUrl())
            .WithTitle("How do I get roles?")
            .WithColor(Color.Aqua)
            .WithDescription(
                $"To give yourself a role, use the `/role-add <roleName>` where **roleName** is whatever role you want.\nTo remove a role, use the `/role-remove <roleName>` replacing **roleName** with the role you want to remove.\n")
            .AddField("Roles available to you:", !roles.Any()
                ? "None"
                : roles.Humanize());
        return Response(new LocalInteractionMessageResponse()
            .WithIsEphemeral()
            .WithEmbeds(roleHelpEmbed));
    }

    [SlashCommand("reaction-role-create")]
    [RequireGuildAdministrator]
    [Description("Creates a reaction role.")]
    public async Task<IResult> CreateReactionRoleAsync(IRole role, ITextChannel channel, string messageId, string emojiId)
    {
        if (!Snowflake.TryParse(messageId, out var snowflakeMessageId))
        {
            return EphmeralFailure("The message ID provided is not a valid ID");
        }

        if (!Snowflake.TryParse(emojiId, out var snowflakeEmojiId))
        {
            return EphmeralFailure("The emoji ID provided is not a valid ID.");
        }

        var message = await channel.FetchMessageAsync(snowflakeMessageId);
        if (message is null)
        {
            return EphmeralFailure("The message ID provided does not exist in the channel provided.");
        }

        var emoji = await Context.Author.GetGuild().FetchEmojiAsync(snowflakeEmojiId);
        if (emoji is null)
        {
            return EphmeralFailure("The emoji ID provided does not exist.");
        }
        
        await message.AddReactionAsync(LocalEmoji.FromEmoji(emoji));
        try
        {
            await _reactionRoleService.CreateReactionRoleAsync(Context.GuildId, snowflakeMessageId, snowflakeEmojiId, role.Id);
            var rr = await _reactionRoleService.FetchReactionRoleAsync(Context.GuildId, snowflakeMessageId, snowflakeEmojiId);
            await message.AddReactionAsync(LocalEmoji.FromEmoji(emoji));
            return Success($"Reaction role created, ID `{rr.Id}`");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("reaction-role-delete")]
    [RequireGuildAdministrator]
    [Description("Deletes a reaction role.")]
    public async Task<IResult> DeleteReactionRoleAsync(long id)
    {
        var reactionRole = await _reactionRoleService.FetchReactionRoleAsync(Context.GuildId, id);
        if (reactionRole is null)
        {
            return EphmeralFailure("No reaction role with that ID exists.");
        }

        try
        {
            await _reactionRoleService.DeleteReactionRoleAsync(reactionRole.GuildId, reactionRole.MessageId, reactionRole.EmojiId, reactionRole.RoleId);
            return Success("Reaction role deleted.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("reaction-roles")]
    [RequireGuildAdministrator]
    [Description("Lists reaction roles.")]
    public async Task<IResult> ListReactionRoleAsync()
    {
        var guild = Context.Author.GetGuild();
        var reactionRoleEmbed = new LocalEmbed()
            .WithTitle("Reaction Roles")
            .WithColor(Color.Purple)
            .WithAuthor(guild.Name, guild.GetIconUrl() ?? null);
        var reactionRoles = await _reactionRoleService.FetchReactionRolesAsync(guild.Id);
        if (!reactionRoles.Any())
        {
            return EphmeralFailure("No reaction roles exist for your guild.");
        }

        foreach (var role in reactionRoles)
        {
            var guildRoles = guild.Roles;
            var roleName = guildRoles.Where(x => x.Value.Id == role.RoleId).FirstOrDefault().Value;
            reactionRoleEmbed.AddField($"({role.Id})Message ID: {role.MessageId}", $"Reacting with <:emoji:{role.EmojiId}> -> will grant **{roleName.Name}**");
        }

        return Response(new LocalInteractionMessageResponse()
            .WithIsEphemeral()
            .WithEmbeds(reactionRoleEmbed));
    }
}