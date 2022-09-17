using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
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
    public IResult FetchGuildCountAsync()
    {
        return Response($"This instance of Unix is currently in {Bot.GetGuilds().Count.ToString()} guilds.");
    }

    [SlashCommand("configure-guild")]
    public async Task<IResult> ConfigureGuildAsync(Snowflake guildId)
    {
        try
        {
            await _guildConfigurationService.CreateGuildConfigurationAsync(guildId);
            return Response("cGuild configuration successfully created.");
        }
        catch (Exception e)
        {
            await Context.SendEphmeralErrorAsync(e.Message);
            return null;
        }
    }

    [SlashCommand("blackiist-guild")]
    public async Task<IResult> BlacklistGuildAsync(Snowflake guildId)
    {
        try
        {
            await _ownerService.BlacklistGuildAsync(guildId);
            return Response("Guild blacklisted.");
        }
        catch (Exception e)
        {
            await Context.SendEphmeralErrorAsync(e.Message);
            return null;
        }
    }
}

