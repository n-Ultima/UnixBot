using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Rest;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.CoreEventHandlers;

public class TagHandler : UnixService
{
    private readonly ITagService _tagService;
    public TagHandler(IServiceProvider serviceProvider, ITagService tagService) : base(serviceProvider)
    {
        _tagService = tagService;
    }


    private static readonly Regex _inlineTagRegex = new(@"\$(\S+)\b");
    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
    {
        if (!eventArgs.GuildId.HasValue)
        {
            return;
        }
        if (eventArgs.Message is not IUserMessage message) return;
        var content = Regex.Replace(message.Content, @"(`{1,3}).*?(.\1)", string.Empty, RegexOptions.Singleline);
        content = Regex.Replace(content, "^>.*$", string.Empty, RegexOptions.Multiline);
        if (string.IsNullOrWhiteSpace(content)) return;
        var match = _inlineTagRegex.Match(content);
        if (!match.Success) return;
        var tagName = match.Groups[1].Value;
        if (string.IsNullOrWhiteSpace(tagName)) return;
        var tag = await _tagService.FetchTagAsync(eventArgs.GuildId.Value, tagName);
        if (tag == null)
        {
            return;
        }

        var reference = message.Reference;
        if (reference == null)
        {
            await eventArgs.Channel.SendMessageAsync(new LocalMessage()
                .WithContent(tag.Content));
            return;
        }
        else
        {
            await eventArgs.Channel.SendMessageAsync(new LocalMessage()
                .WithContent(tag.Content)
                .WithReference(new LocalMessageReference()
                    .WithGuildId(eventArgs.GuildId.Value)
                    .WithChannelId(eventArgs.ChannelId)
                    .WithMessageId(reference.MessageId.Value)));
        }
    }
}