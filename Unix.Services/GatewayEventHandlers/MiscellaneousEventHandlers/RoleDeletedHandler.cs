using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Serilog;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.MiscellaneousEventHandlers;

public class RoleDeletedHandler : UnixService
{
    private readonly IGuildService _guildService;

    public RoleDeletedHandler(IServiceProvider serviceProvider, IGuildService guildService) : base(serviceProvider)
    {
        _guildService = guildService;
    }

    protected override async ValueTask OnRoleDeleted(RoleDeletedEventArgs eventArgs)
    {
        var guildConfig = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId);
        if (guildConfig == null)
        {
            return;
        }

        if (guildConfig.MiscellaneousLogChannelId == default)
        {
            return;
        }

        if (guildConfig.SelfAssignableRoles.Contains(eventArgs.RoleId.RawValue))
        {
            Log.Logger.Information("Role {rName} was deleted, and also present in the guild's self assignable roles. Removing...", eventArgs.Role.Name);
            await _guildService.RemoveSelfAssignableRoleAsync(guildConfig.Id, eventArgs.RoleId);
            Log.Logger.Information("Role removed successfully!");
        }

        if (guildConfig.AutoRoles.Contains(eventArgs.RoleId.RawValue))
        {
            Log.Logger.Information("Role {rName} was deleted, and also present in the guild's autoroles. Removing...", eventArgs.Role.Name);
            await _guildService.RemoveAutoRoleAsync(guildConfig.Id, eventArgs.RoleId);
            Log.Logger.Information("Role removed successfully!");
        }
        var auditLogs = await Bot.FetchAuditLogsAsync<IRoleDeletedAuditLog>(eventArgs.GuildId);
        var roleDeletedAuditLog = auditLogs.First();
        await Bot.SendMessageAsync(guildConfig.MiscellaneousLogChannelId, new LocalMessage()
            .WithContent($"Role **{eventArgs.Role.Name ?? "Not cached"}**(`{eventArgs.RoleId}` was deleted by **{roleDeletedAuditLog.Actor.Tag}**(`{roleDeletedAuditLog.Actor.Id}`)"));
    }
}