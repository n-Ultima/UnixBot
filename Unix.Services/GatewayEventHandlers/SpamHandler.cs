using System;
using System.Collections.Concurrent;
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

namespace Unix.Services.GatewayEventHandlers
{
    public class SpamHandler : UnixService
    {
        private ConcurrentDictionary<CachedMember, int> SpamDictionary = new();
        public static ConcurrentDictionary<Snowflake, int> AmountOfMessages = new();
        private readonly ModerationService _moderationService;
        private readonly GuildService _guildService;
        private Timer SpamTimer;
        public SpamHandler(IServiceProvider serviceProvider, GuildService guildService, ModerationService moderationService) : base(serviceProvider)
        {
            _guildService = guildService;
            _moderationService = moderationService;
            SetTimer();
        }

        private void SetTimer()
        {
            var timeSpan = TimeSpan.FromSeconds(3);
            SpamTimer = new Timer(_ => Task.Run(HandleTimerAsync), null, timeSpan, timeSpan);
        }

        private async Task HandleTimerAsync()
        {
            foreach (var entry in SpamDictionary)
            {
                var member = entry.Key; // The member
                var guild = member.GetGuild(); // The guild that the member resides in. Now have Member, Guild
                var guildConfig = await _guildService.FetchGuildConfigurationAsync(guild.Id); // Get the guild config
                var spamAmt = AmountOfMessages[guildConfig.Id];// The amount of messages considered spam. 
                var warnUsers = SpamDictionary
                    .Where(x => x.Value >= spamAmt)
                    .Where(x => x.Key.GuildId == guildConfig.Id)
                    .ToList();
                foreach (var user in warnUsers)
                {
                    if (!SpamDictionary.TryGetValue(user.Key, out _))
                    {
                        continue;
                    }
                    await _moderationService.CreateInfractionAsync(user.Key.GuildId, Bot.CurrentUser.Id, user.Key.Id, InfractionType.Warn, "Spamming messages", null);
                    if(!SpamDictionary.TryRemove(user.Key, out _))
                    {
                        Log.Logger.Error("Failed to remove {key} from the spam dictionary.", user.Key);
                    }
                }

                var removeUsers = SpamDictionary
                    .Where(x => x.Value < spamAmt)
                    .Where(x => x.Key.GuildId == guildConfig.Id)
                    .ToList();
                foreach (var user in removeUsers)
                {
                    if (!SpamDictionary.TryRemove(user.Key, out _))
                    {
                        Log.Logger.Error("Failed to remove {key} from the spam dictionary.", user.Key);
                    }
                }
            }
        }

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Channel == null)
            {
                return;
            }
            
            if (eventArgs.Message is not IUserMessage message) return;
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
            var guild = Bot.GetGuild(guildConfig.Id);
            if (!eventArgs.Member.RoleIds.Any())
            {
                goto botCheck;
            }
            if (eventArgs.Member.RoleIds.Contains(guildConfig.ModeratorRoleId) || eventArgs.Member.RoleIds.Contains(guildConfig.AdministratorRoleId))
            {
                return;
            }
            botCheck:
            if (message.Author.IsBot)
            {
                return;
            }
            SpamDictionary.AddOrUpdate(eventArgs.Member, 1, (_, oldValue) => oldValue + 1);
        }
    }
}