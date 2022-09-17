using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Unix.Data.Models.Core;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules.Bases;

public abstract class UnixModuleBase : DiscordApplicationGuildModuleBase
{
    private readonly IGuildService _guildConfigurationService;
    public GuildConfiguration CurrentGuildConfiguration { get; set; }

    public UnixModuleBase(IGuildService guildConfigurationService)
    {
        _guildConfigurationService = guildConfigurationService;
    }
    public override async ValueTask OnBeforeExecuted()
    {
        if (Context.Interaction is not ISlashCommandInteraction)
        {
            return;
            //TODO: In the event that I implement modals, or anything besides slash commands, this must be changed.
        }

        var guildConfig = await _guildConfigurationService.FetchGuildConfigurationAsync(Context.GuildId);
        if (guildConfig is null)
        {
            Log.Logger.ForContext<UnixModuleBase>().Error("Interaction was attempted in a guild without a proper guild configuration setup.");
            return;
        }

        CurrentGuildConfiguration = guildConfig;

    }
}