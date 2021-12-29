using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Rest;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.CoreEventHandlers;

public class QuoteHandler : UnixService
{
    private readonly IGuildService _guildService;

    private readonly Regex _messageRegex = new(@"https?://(?:(ptb|canary)\.)?discord(?:app)?\.com/channels/(?<guild_id>([0-9]{15,21})|(@me))/(?<channel_id>[0-9]{15,21})/(?<message_id>[0-9]{15,21})/?", RegexOptions.Compiled);

    public QuoteHandler(IGuildService guildService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _guildService = guildService;
    }

    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
    {
        if (!eventArgs.GuildId.HasValue)
        {
            return;
        }

        var guildConfig = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId.Value);

        if (guildConfig == null)
        {
            return;
        }

        if (!_messageRegex.IsMatch(eventArgs.Message.Content))
        {
            return;
        }

        var match = _messageRegex.Match(eventArgs.Message.Content);

        var matchedGuildId = match.Groups["guild_id"].Value;

        if (matchedGuildId == "@me")
        {
            return;
        }

        var guildId = Convert.ToUInt64(matchedGuildId);

        if (eventArgs.GuildId.Value != guildId)
        {
            return;
        }

        var channelId = Convert.ToUInt64(match.Groups["channel_id"].Value);
        var messageId = Convert.ToUInt64(match.Groups["message_id"].Value);

        var message = await Bot.FetchMessageAsync(channelId, messageId);

        var eb = new LocalEmbed()
            .WithColor(Color.Gold)
            .WithAuthor(message.Author)
            .AddField("Message", message.Content)
            .AddField("Quoted by", eventArgs.Message.Author.Mention)
            .AddField("Jump", match.Value);

        await eventArgs.Channel.SendMessageAsync(new LocalMessage().WithEmbeds(eb));
    }
}