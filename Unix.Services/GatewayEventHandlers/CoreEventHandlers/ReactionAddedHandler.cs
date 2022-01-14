using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Serilog;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.CoreEventHandlers;

public class ReactionAddedHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly IReactionRoleService _reactionRoleService;
    
    public ReactionAddedHandler(IServiceProvider serviceProvider, IGuildService guildService, IReactionRoleService reactionRoleService) : base(serviceProvider)
    {
        _guildService = guildService;
        _reactionRoleService = reactionRoleService;
    }


    protected override async ValueTask OnReactionAdded(ReactionAddedEventArgs eventArgs)
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

        var reactionRole = await _reactionRoleService.FetchReactionRoleAsync(guildConfig.Id, eventArgs.MessageId, (eventArgs.Emoji as ICustomEmoji).Id);
        if (reactionRole == null)
        {
            return;
        }

        await eventArgs.Member.GrantRoleAsync(reactionRole.RoleId, new DefaultRestRequestOptions {Reason = $"Reaction role"});
    }
}