using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.MiscellaneousEventHandlers;

public class MemberJoinedHandler : UnixService
{
    private readonly IGuildService _guildService;

    public MemberJoinedHandler(IServiceProvider serviceProvider, IGuildService guildService) : base(serviceProvider)
    {
        _guildService = guildService;
    }

    protected override async ValueTask OnMemberJoined(MemberJoinedEventArgs eventArgs)
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

        await Bot.SendMessageAsync(guildConfig.MiscellaneousLogChannelId, new LocalMessage()
            .WithEmbeds(new LocalEmbed()
                .WithAuthor(eventArgs.Member)
                .WithTitle("Member Joined")
                .WithThumbnailUrl(eventArgs.Member.GetAvatarUrl() ?? eventArgs.Member.GetDefaultAvatarUrl())
                .AddField("Username and Discriminator", eventArgs.Member.Tag)
                .AddField("Account Age", (DateTimeOffset.UtcNow - eventArgs.Member.CreatedAt()).Humanize())));
    }
}