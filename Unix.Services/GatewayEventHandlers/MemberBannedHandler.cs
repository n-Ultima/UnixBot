using System;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Unix.Data;
using Unix.Data.Models.Moderation;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers;

public class MemberBannedHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly IModerationService _moderationService;
    public MemberBannedHandler(IServiceProvider serviceProvider, IGuildService guildService, IModerationService moderationService) : base(serviceProvider)
    {
        _guildService = guildService;
        _moderationService = moderationService;
    }

    protected override async ValueTask OnBanCreated(BanCreatedEventArgs eventArgs)
    {
        var guildConfig = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId);
        if (guildConfig == null)
        {
            return;
        }

        var memberBannedInfractions = await _moderationService.FetchInfractionsAsync(eventArgs.GuildId, eventArgs.UserId);
        var memberBannedInfraction = memberBannedInfractions
            .Where(x => x.Type == InfractionType.Ban)
            .FirstOrDefault();
        if (memberBannedInfraction == null)
        {
            var banAuditLogs = await Bot.FetchAuditLogsAsync<IMemberBannedAuditLog>(eventArgs.GuildId);
            var banAuditLog = banAuditLogs
                .Where(x => x.TargetId == eventArgs.UserId)
                .FirstOrDefault();
            if (banAuditLog == null)
            {
                // Why do we have no audit log, but we received it? Discord's fuck up, not our job to handle. 
                return;
            }

            await _moderationService.CreateInfractionAsync(eventArgs.GuildId, banAuditLog.ActorId.Value, banAuditLog.TargetId.Value, InfractionType.Ban, banAuditLog.Reason ?? "No reason provided(manual ban).", true, null);
        }
        else
        {
            // Well we have an infraction, which means that this was done via command.
            return;
        }
    }
}