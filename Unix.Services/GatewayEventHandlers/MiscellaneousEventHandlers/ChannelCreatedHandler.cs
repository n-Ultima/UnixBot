using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.MiscellaneousEventHandlers;

public class ChannelCreatedHandler : UnixService
{
    private readonly IGuildService _guildService;
    public ChannelCreatedHandler(IGuildService guildService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _guildService = guildService;
    }

    protected override async ValueTask OnChannelCreated(ChannelCreatedEventArgs eventArgs)
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

        var auditLogs = await Bot.FetchAuditLogsAsync<IChannelCreatedAuditLog>(eventArgs.GuildId);
        var channelCreateLog = auditLogs.First();
        await Bot.SendMessageAsync(guildConfig.MiscellaneousLogChannelId, new LocalMessage()
            .WithContent($"Channel {Mention.Channel(eventArgs.ChannelId)}(#{eventArgs.Channel.Name}, `{eventArgs.ChannelId}`) was created by **{channelCreateLog.Actor.Tag}**(`{channelCreateLog.Actor.Id}`)"));
    }
}