using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Data.Models.Moderation;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.ModerationEventHandlers;

public class MemberTimedOutHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly IModerationService _moderationService;

    public MemberTimedOutHandler(IGuildService guildService, IModerationService moderationService, IServiceProvider serviceProvider) : base(serviceProvider)
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
        var memberTimedOutInfraction = memberInfractions
            .Where(x => x.Type == InfractionType.Mute)
            .FirstOrDefault();
        if (memberTimedOutInfraction == null)
        {
            var memberUpdatedAuditLogs = await Bot.FetchAuditLogsAsync<IMemberUpdatedAuditLog>(eventArgs.GuildId);
            var memberUpdatedAuditLog = memberUpdatedAuditLogs
                .Where(x => x.TargetId == eventArgs.MemberId)
                .FirstOrDefault();
            if (memberUpdatedAuditLog == null)
            {
                // Discord fuck up, not our issue.
                return;
            }

            if (!eventArgs.NewMember.TimedOutUntil.HasValue || eventArgs.NewMember.TimedOutUntil.Value < DateTimeOffset.UtcNow)
            {
                return;
            }
            await _moderationService.CreateInfractionAsync(eventArgs.GuildId, memberUpdatedAuditLog.ActorId.Value, memberUpdatedAuditLog.TargetId.Value, InfractionType.Mute, memberUpdatedAuditLog.Reason ?? "No reason provided(manual timeout).", true, eventArgs.NewMember.TimedOutUntil.Value - DateTimeOffset.UtcNow);
        }
    }
}