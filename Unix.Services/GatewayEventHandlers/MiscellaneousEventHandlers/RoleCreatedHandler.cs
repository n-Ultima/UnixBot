using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.MiscellaneousEventHandlers;

public class RoleCreatedHandler : UnixService
{
    private readonly IGuildService _guildService;

    public RoleCreatedHandler(IServiceProvider serviceProvider, IGuildService guildService) : base(serviceProvider)
    {
        _guildService = guildService;
    }

    protected override async ValueTask OnRoleCreated(RoleCreatedEventArgs eventArgs)
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

        var auditLogs = await Bot.FetchAuditLogsAsync<IRoleCreatedAuditLog>(eventArgs.GuildId);
        var roleCreateLog = auditLogs.First();
        await Bot.SendMessageAsync(guildConfig.MiscellaneousLogChannelId, new LocalMessage()
            .WithContent($"Role **{eventArgs.Role.Name}**(`{eventArgs.RoleId}`) was created by **{roleCreateLog.Actor.Tag}**(`{roleCreateLog.Actor.Id}`)"));
    }
}