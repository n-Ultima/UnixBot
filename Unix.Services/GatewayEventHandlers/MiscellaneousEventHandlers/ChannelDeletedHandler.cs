using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.Logging;
using Serilog;
using Unix.Services.Core.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Unix.Services.GatewayEventHandlers.MiscellaneousEventHandlers;

public class ChannelDeletedHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly ILogger<ChannelDeletedHandler> _logger;
    public ChannelDeletedHandler(IGuildService guildService, ILogger<ChannelDeletedHandler> logger, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _guildService = guildService;
        _logger = logger;
    }

    protected override async ValueTask OnChannelDeleted(ChannelDeletedEventArgs eventArgs)
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

        if (guildConfig.ModLogChannelId == eventArgs.ChannelId)
        {
            _logger.LogInformation("Channel {cName} was deleted, and also present as the guild's modlog channel ID. Removing...", eventArgs.Channel.Name);
            await _guildService.ModifyGuildModLogChannelIdAsync(eventArgs.GuildId, default);
            _logger.LogInformation("Channel removed successfully!");
        }

        if (guildConfig.MessageLogChannelId == eventArgs.ChannelId)
        {
            _logger.LogInformation("Channel {cName} was deleted, and also present as the guild's message log channel ID. Removing...", eventArgs.Channel.Name);
            await _guildService.ModifyGuildMessageLogChannelIdAsync(eventArgs.GuildId, default);
            _logger.LogInformation("Channel removed successfully!");
        }

        if (guildConfig.MiscellaneousLogChannelId == eventArgs.ChannelId)
        {
            _logger.LogInformation("Channel {cName} was deleted, and also present as the guild's miscellaneous log channel ID. Removing...(aborting the log)", eventArgs.Channel.Name);
            await _guildService.ModifyGuildMiscellaneousLogChannelIdAsync(eventArgs.GuildId, default);
            _logger.LogInformation("Channel removed successfully!");
            return;
        }
        var auditLogs = await Bot.FetchAuditLogsAsync<IChannelDeletedAuditLog>(eventArgs.GuildId);
        var channelDeleteLog = auditLogs.First();
        if (eventArgs.Channel.Type == ChannelType.Category)
        {
            await Bot.SendMessageAsync(guildConfig.MiscellaneousLogChannelId, new LocalMessage()
                .WithContent($"Category channel **{eventArgs.Channel.Name}**(`{eventArgs.ChannelId}`) was deleted by **{channelDeleteLog.Actor.Tag}**(`{channelDeleteLog.Actor.Id}`)"));
            return;
        }
        await Bot.SendMessageAsync(guildConfig.MiscellaneousLogChannelId, new LocalMessage()
            .WithContent($"Channel **#{eventArgs.Channel.Name}**(`{eventArgs.ChannelId}`) was deleted by **{channelDeleteLog.Actor.Tag}**(`{channelDeleteLog.Actor.Id}`)"));
    }
}