using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.Gateway;
using Unix.Common;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules.Bases;

public abstract class UnixModeratorModuleBase : UnixModuleBase
{
    protected UnixModeratorModuleBase(IGuildService guildConfigurationService) : base(guildConfigurationService)
    {
    }
    
    public override async ValueTask OnBeforeExecuted()
    {
        if ((Context.Author.GetGuild()?.OwnerId != Context.AuthorId) || !Context.Author.RoleIds.Contains(CurrentGuildConfiguration.ModeratorRoleId.RawValue) || !Context.Author.RoleIds.Contains(CurrentGuildConfiguration.AdministratorRoleId.RawValue) || (Context.Author.RoleIds.Count is 0))
        {
            await Context.SendEphmeralErrorAsync(PermissionLevel.Moderator);
            throw new Exception("Missing permissions.");
        }
    }
}