﻿using System;
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

[SlashGroup("role")]
public class RoleModule : UnixModuleBase
{
    private readonly IReactionRoleService _reactionRoleService;
    public RoleModule(IGuildService guildConfigurationService, IReactionRoleService reactionRoleService) : base(guildConfigurationService)
    {
        _reactionRoleService = reactionRoleService;
    }
    
    [SlashCommand("list")]
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

    [SlashCommand("add")]
    [Description("Give yourself a role.")]
    public async Task<IResult> AddRoleAsync(IRole role)
    {
        if (!CurrentGuildConfiguration.SelfAssignableRoles.Contains(role.Id.RawValue))
        {
            return EphmeralFailure("Unknown role.");
        }

        if (Context.Author.RoleIds.Contains(role.Id))
        {
            return EphmeralFailure("You already have that role.");
        }

        await Context.Author.GrantRoleAsync(role.Id);
        return Success($"Granted you **{role.Name}**.");
    }

    [SlashCommand("remove")]
    [Description("Take away a role.")]
    public async Task<IResult> RemoveRoleAsync(IRole role)
    {
        if (!CurrentGuildConfiguration.SelfAssignableRoles.Contains(role.Id.RawValue))
        {
            return EphmeralFailure("Unknown role.");
        }

        if (!Context.Author.RoleIds.Contains(role.Id))
        {
            return EphmeralFailure("You don't have that role.");
        }
        await Context.Author.RevokeRoleAsync(role.Id);
        return Success($"Removed **{role.Name}** from you.");
    }
}