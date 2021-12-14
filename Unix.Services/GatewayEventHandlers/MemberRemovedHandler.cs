using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Data.Models.Moderation;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers;

public class MemberRemovedHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly IModerationService _moderationService;

    public MemberRemovedHandler(IGuildService guildService, IModerationService moderationService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _guildService = guildService;
        _moderationService = moderationService;
    }

    protected override async ValueTask OnMemberLeft(MemberLeftEventArgs eventArgs)
    {
        var guildConfig = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId);
        if (guildConfig == null)
        {
            return;
        }

        var kickedAuditLogs = await Bot.FetchAuditLogsAsync<IMemberKickedAuditLog>(eventArgs.GuildId);
        var memberKickedAuditLog = kickedAuditLogs
            .Where(x => x.TargetId == eventArgs.MemberId)
            .FirstOrDefault();
        if (memberKickedAuditLog == null)
        {
            return;
        }
        var memberKickedInfractions = await _moderationService.FetchInfractionsAsync(eventArgs.GuildId, eventArgs.MemberId);
        var memberKickedInfraction = memberKickedInfractions
            .Where(x => x.Type == InfractionType.Kick)
            .FirstOrDefault();
        if (memberKickedInfraction == null)
        {
            // User was kicked via the client.
            await _moderationService.CreateInfractionAsync(eventArgs.GuildId, memberKickedAuditLog.ActorId.Value, eventArgs.MemberId, InfractionType.Kick, memberKickedAuditLog.Reason ?? "No reason provided(manual kick).", true, null);
        }
        else
        {
            // User was kicked via command, no need to do anything.
        }

    }
}