﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Disqord.Gateway;
using Unix.Common;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules.Bases;

public abstract class UnixAdministratorModuleBase : UnixModuleBase
{
    protected UnixAdministratorModuleBase(IGuildService guildConfigurationService) : base(guildConfigurationService)
    {
    }

    public override async ValueTask OnBeforeExecuted()
    {
        // fuck
        await base.OnBeforeExecuted();
        if(CurrentGuildConfiguration is null) Console.WriteLine("Fuck its null");
        if(CurrentGuildConfiguration.AdministratorRoleId.RawValue == default) Console.WriteLine("Fuck its default");
        if ((CurrentGuildConfiguration.AdministratorRoleId.RawValue == default) || (Context.Author.GetGuild()?.OwnerId != Context.AuthorId) || (!Context.Author.RoleIds.Contains(CurrentGuildConfiguration.AdministratorRoleId.RawValue)) ||
            (Context.Author.RoleIds.Count is 0))
        {
            await Context.SendEphmeralErrorAsync(PermissionLevel.Administrator);
            throw new Exception("Missing permissions.");
        }
    }
    
}