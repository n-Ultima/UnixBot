using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Data.Models.Moderation;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers;

public class MemberUnbannedHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly IModerationService _moderationService;
    public MemberUnbannedHandler(IServiceProvider serviceProvider, IGuildService guildService, IModerationService moderationService) : base(serviceProvider)
    {
        _guildService = guildService;
        _moderationService = moderationService;
    }

    protected override async ValueTask OnBanDeleted(BanDeletedEventArgs eventArgs)
    {
        var guildConfig = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId);
        if (guildConfig == null)
        {
            return;
        }

        var memberUnbannedInfractions = await _moderationService.FetchInfractionsAsync(eventArgs.GuildId, eventArgs.UserId);
        var memberUnbannedInfraction = memberUnbannedInfractions
            .Where(x => x.Type == InfractionType.Ban)
            .SingleOrDefault();
        if (memberUnbannedInfraction != null)
        {
            // Member was unbanned manually.
            var memberUnbannedAuditLogs = await Bot.FetchAuditLogsAsync<IMemberUnbannedAuditLog>(eventArgs.GuildId, 1);
            var memberUnbannedAuditLog = memberUnbannedAuditLogs
                .Where(x => x.TargetId == eventArgs.UserId)
                .FirstOrDefault();
            if (memberUnbannedAuditLog == null)
            {
                return;
            }

            await _moderationService.RemoveInfractionAsync(memberUnbannedInfraction.Id, eventArgs.GuildId, memberUnbannedAuditLog.ActorId.Value, true, memberUnbannedAuditLog.Reason ?? "No reason provided(manual unban).");
        }
        else
        {
            return;
        }
    }
}