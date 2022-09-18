using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules;

public class GuildOwnerModule : UnixModuleBase
{
    public GuildOwnerModule(IGuildService guildConfigurationService) : base(guildConfigurationService)
    {
    }

    [SlashCommand("configure-modrole")]
    [RequireGuildOwner]
    [Description("Sets the moderator role of the guild.")]
    public async Task<IResult> SetModRoleAsync(IRole role)
    {
        try
        {
            await _guildConfigurationService.ModifyGuildModRoleAsync(Context.GuildId, role.Id);
            return Success($"Moderator role set to **{role.Name}**.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("configure-adminrole")]
    [RequireGuildOwner]
    [Description("Sets the administrator role of the guild.")]
    public async Task<IResult> SetAdminRoleAsync(IRole role)
    {
        try
        {
            await _guildConfigurationService.ModifyGuildAdminRoleAsync(Context.GuildId, role.Id);
            return Success($"Administrator role set to **{role.Name}**");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }
}