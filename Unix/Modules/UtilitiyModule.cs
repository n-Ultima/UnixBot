using Disqord.Bot.Commands.Application;
using Qmmands;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules;

public class UtilitiyModule : UnixModuleBase
{
    public UtilitiyModule(IGuildService guildConfigurationService) : base(guildConfigurationService)
    {
    }

    [SlashCommand("ping")]
    [Description("Find out.")]
    public IResult Ping()
    {
        return Response("Pong!");
    }
}