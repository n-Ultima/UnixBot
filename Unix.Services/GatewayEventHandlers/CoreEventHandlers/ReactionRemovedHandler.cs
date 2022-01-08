using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.CoreEventHandlers;

public class ReactionRemovedHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly IReactionRoleService _reactionRoleService;

    public ReactionRemovedHandler(IGuildService guildService, IReactionRoleService reactionRoleService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _guildService = guildService;
        _reactionRoleService = reactionRoleService;
    }

    protected override async ValueTask OnReactionRemoved(ReactionRemovedEventArgs eventArgs)
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

        var reactionRole = await _reactionRoleService.FetchReactionRoleAsync(guildConfig.Id, eventArgs.MessageId, (eventArgs.Emoji as IGuildEmoji).Id);
        if (reactionRole == null)
        {
            return;
        }

        await Bot.RevokeRoleAsync(guildConfig.Id, eventArgs.UserId, reactionRole.RoleId, new DefaultRestRequestOptions {Reason = "Reaction role"});
    }
}