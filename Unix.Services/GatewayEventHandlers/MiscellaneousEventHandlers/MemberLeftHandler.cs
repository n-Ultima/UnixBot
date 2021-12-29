using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.MiscellaneousEventHandlers;

public class MemberLeftHandler : UnixService
{
    private readonly IGuildService _guildService;

    public MemberLeftHandler(IServiceProvider serviceProvider, IGuildService guildService) : base(serviceProvider)
    {
        _guildService = guildService;
    }

    protected override async ValueTask OnMemberLeft(MemberLeftEventArgs eventArgs)
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
                .WithAuthor(eventArgs.User)
                .WithTitle("Member Left")
                .WithThumbnailUrl(eventArgs.User.GetAvatarUrl() ?? eventArgs.User.GetDefaultAvatarUrl())
                .AddField("Username and Discriminator", eventArgs.User.Tag)
                .AddField("Account Age", (DateTimeOffset.UtcNow - eventArgs.User.CreatedAt()).Humanize())));
    }
}