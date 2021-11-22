using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Common;
using Unix.Services.Core;
using Unix.Services.Extensions;

namespace Unix.Services.GatewayEventHandlers;

public class InteractionHandler : UnixService
{
    private readonly OwnerService _ownerService;
    private readonly GuildService _guildService;
    private readonly ModerationService _moderationService;
    
    public InteractionHandler(IServiceProvider serviceProvider, OwnerService ownerService, GuildService guildService, ModerationService moderationService) : base(serviceProvider)
    {
        _ownerService = ownerService;
        _guildService = guildService;
        _moderationService = moderationService;
    }

    protected async override ValueTask OnInteractionReceived(InteractionReceivedEventArgs eventArgs)
    {
        if (eventArgs.Interaction.Type != InteractionType.ApplicationCommand)
        {
            return;
        }

        if (eventArgs.Interaction is not ISlashCommandInteraction slashCommandInteraction)
        {
            return;
        }

        if (!eventArgs.GuildId.HasValue)
        {
            return;
        }

        switch (slashCommandInteraction.CommandName)
        {
            case "ping":
                var dateTime = DateTimeOffset.UtcNow - eventArgs.Interaction.CreatedAt();
                var heartbeatLatency = eventArgs.Interaction.GetGatewayClient().ApiClient.Heartbeater.Latency;
                var builder = new StringBuilder();
                if (!heartbeatLatency.HasValue)
                {
                    builder.Append($"🏓 Pong!\nShard Latency: {Bot.GetShard(eventArgs.GuildId.Value).Heartbeater.Latency.Value.Milliseconds} ms\nMessage Latency: {dateTime.Milliseconds} ms");
                }
                else
                {
                    builder.Append($"🏓 Pong!\nDirect API Latency: {heartbeatLatency.Value.Milliseconds} ms\nShard Latency: {Bot.GetShard(eventArgs.GuildId.Value).Heartbeater.Latency.Value.Milliseconds} ms\nMessage Latency: {dateTime.Milliseconds} ms");
                }

                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithContent(builder.ToString()));
                break;
            case "guild-count":
                if (!Bot.OwnerIds.Contains(eventArgs.Member.Id))
                {
                    await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                        .WithIsEphemeral()
                        .WithContent("You must be a bot owner to execute this command."));
                    break;
                }

                await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionResponse()
                    .WithContent(Bot.GetGuilds().Count.ToString()));
                break;
            case "configure-muterole":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var muteRole = slashCommandInteraction.Entities.Roles.First().Value;
                await _guildService.ModifyGuildMuteRoleIdAsync(eventArgs.GuildId.Value, muteRole.Id);
                await eventArgs.SendSuccessAsync($"Muterole configured to **{muteRole.Name}**");
                break;
            case "configure-modrole":
                if (!eventArgs.Member.IsAdmin())
                {
                    await eventArgs.SendEphmeralErrorAsync(PermissionLevel.Administrator);
                    break;
                }

                var modRole = slashCommandInteraction.Entities.Roles.First().Value;
                await _guildService.ModifyGuildModRoleAsync(eventArgs.GuildId.Value, modRole.Id);
                await eventArgs.SendSuccessAsync($"Moderator role set to **{modRole.Name}**");
                break;
        }
    }
}