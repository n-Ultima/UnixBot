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