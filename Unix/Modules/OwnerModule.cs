using System;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Humanizer;
using Qmmands;
using Unix.Common;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules;

public class OwnerModule : UnixOwnerModuleBase
{
    private readonly IOwnerService _ownerService;

    public OwnerModule(IGuildService guildConfigurationService, IOwnerService ownerService) : base(guildConfigurationService)
    {
        _ownerService = ownerService;
    }

    [SlashCommand("guild-count")]
    [Description("Returns the amount of guilds that this instance of Unix is currently in.")]
    public IResult FetchGuildCountAsync()
    {
        return Response($"This instance of Unix is currently in {Bot.GetGuilds().Count.ToString()} guilds.");
    }

    [SlashCommand("configure-guild")]
    [Description("Creates an empty guild configuration for the guild.")]
    public async Task<IResult> ConfigureGuildAsync(Snowflake guildId)
    {
        try
        {
            await _guildConfigurationService.CreateGuildConfigurationAsync(guildId);
            return Success("Guild configuration created successfully.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("blacklist-guild")]
    [Description("Disallows a guild from using Unix.")]
    public async Task<IResult> BlacklistGuildAsync(Snowflake guildId)
    {
        try
        {
            await _ownerService.BlacklistGuildAsync(guildId);
            return Success("Guild blacklisted.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }
}

