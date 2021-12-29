using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Data.Models.Moderation;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.ModerationEventHandlers;

public class MemberTimeOutRemovedHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly IModerationService _moderationService;

    public MemberTimeOutRemovedHandler(IGuildService guildService, IModerationService moderationService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _guildService = guildService;
        _moderationService = moderationService;
    }

    protected override async ValueTask OnMemberUpdated(MemberUpdatedEventArgs eventArgs)
    {
        var guildConfig = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId);
        if (guildConfig == null)
        {
            return;
        }

        var memberInfractions = await _moderationService.FetchInfractionsAsync(eventArgs.GuildId, eventArgs.MemberId);
        var memberInfraction = memberInfractions
            .Where(x => x.Type == InfractionType.Mute)
            .FirstOrDefault();
        if (memberInfraction != null)
        {
            var memberUpdatedAuditLogs = await Bot.FetchAuditLogsAsync<IMemberUpdatedAuditLog>(eventArgs.GuildId);
            var memberUpdatedAuditLog = memberUpdatedAuditLogs
                .Where(x => x.TargetId == eventArgs.MemberId)
                .FirstOrDefault();
            if (memberUpdatedAuditLog == null)
            {
                return;
            }

            if (eventArgs.NewMember.TimedOutUntil.HasValue || eventArgs.NewMember.TimedOutUntil! < DateTimeOffset.UtcNow)
            {
                return;
            }

            await _moderationService.RemoveInfractionAsync(memberInfraction.Id, memberInfraction.GuildId, memberUpdatedAuditLog.ActorId.Value, true, memberUpdatedAuditLog.Reason ?? "No reason provided(timeout removed manually).");
        }
    }
}