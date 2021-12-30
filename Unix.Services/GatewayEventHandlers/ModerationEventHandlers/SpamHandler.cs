using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Serilog;
using Unix.Data.Models.Core;
using Unix.Data.Models.Moderation;
using Unix.Services.Core;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers;

public class SpamHandler : UnixService
{
    private readonly ConcurrentDictionary<CachedMember, List<IUserMessage>> SpamDictionary = new();
    public static ConcurrentDictionary<Snowflake, int> AmountOfMessages = new();
    private readonly IModerationService _moderationService;
    private readonly IGuildService _guildService;

    public SpamHandler(IServiceProvider serviceProvider, IGuildService guildService, IModerationService moderationService) : base(serviceProvider)
    {
        _guildService = guildService;
        _moderationService = moderationService;
    }

    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
    {
        if (eventArgs.Channel == null)
        {
            return;
        }

        if (eventArgs.Message is not IUserMessage message) return;
        if (eventArgs.Member == null)
        {
            return;
        }

        if (!AmountOfMessages.TryGetValue(eventArgs.GuildId.Value, out _))
        {
            var guildConfigTemp = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId.Value);
            if (guildConfigTemp == null)
            {
                return;
            }

            AmountOfMessages.TryAdd(eventArgs.GuildId.Value, guildConfigTemp.AmountOfMessagesConsideredSpam);
        }

        var guildConfig = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId.Value);
        if (guildConfig == null)
        {
            return;
        }

        if (!eventArgs.Member.RoleIds.Any())
        {
            goto botCheck;
        }

        if (eventArgs.Member.RoleIds.Contains(guildConfig.ModeratorRoleId) || eventArgs.Member.RoleIds.Contains(guildConfig.AdministratorRoleId) || eventArgs.Member.GetGuild().OwnerId == eventArgs.Member.Id)
        {
            return;
        }

        botCheck:
        if (message.Author.IsBot)
        {
            return;
        }

        if (guildConfig.AmountOfMessagesConsideredSpam == 0)
        {
            return;
        }

        var spamAmt = SpamDictionary.TryGetValue(eventArgs.Member, out var messages);
        if (!spamAmt)
        {
            List<IUserMessage> userMessages = new();
            userMessages.Add(message);
            SpamDictionary.TryAdd(eventArgs.Member, userMessages);
        }
        else
        {
            List<IUserMessage> updatedMessages = new();
            updatedMessages.AddRange(messages);
            updatedMessages.Add(message);
            SpamDictionary[eventArgs.Member] = updatedMessages;
            if (SpamDictionary[eventArgs.Member].Count == 3)
            {
                await CheckIfSpamAsync(eventArgs.Member, SpamDictionary[eventArgs.Member]);
            }
        }
    }

    private async Task CheckIfSpamAsync(CachedMember member, List<IUserMessage> messages)
    {
        if (messages.Count != 3)
        {
            throw new Exception("Method must be called when the user has sent at least 3 messages.");
        }

        var message1 = messages[0];
        var message2 = messages[1];
        var message3 = messages[2];
        if (message3.CreatedAt() - message2.CreatedAt() < TimeSpan.FromSeconds(1) && message2.CreatedAt() - message1.CreatedAt() < TimeSpan.FromSeconds(1))
        {
            await _moderationService.CreateInfractionAsync(member.GuildId, Bot.CurrentUser.Id, member.Id, InfractionType.Warn, "Spamming messages", false, null);
            SpamDictionary.TryRemove(member, out _);
        }
    }
}