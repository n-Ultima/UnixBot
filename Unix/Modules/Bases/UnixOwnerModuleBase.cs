using System;
using System.Threading.Tasks;
using Unix.Common;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules.Bases;

public class UnixOwnerModuleBase : UnixModuleBase
{
    public UnixOwnerModuleBase(IGuildService guildConfigurationService) : base(guildConfigurationService)
    {
    }

    public override async ValueTask OnBeforeExecuted()
    {
        if (!await Bot.IsOwnerAsync(Context.AuthorId))
        {
            await Context.SendEphmeralErrorAsync("⚠ You must be a bot owner to execute this command.");
            throw new Exception("Missing permissions.");
        }
    }
}