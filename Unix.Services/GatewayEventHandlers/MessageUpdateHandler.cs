using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Unix.Data.Models.Core;
using Unix.Services.Core;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers
{
    public class MessageUpdateHandler : UnixService
    {
        private readonly IGuildService _guildService;
        private readonly IModerationService _moderationService;
        private readonly HttpClient _httpClient;

        public MessageUpdateHandler(IServiceProvider serviceProvider, IGuildService guildService, IModerationService moderationService, HttpClient httpClient) : base(serviceProvider)
        {
            _guildService = guildService;
            _moderationService = moderationService;
            _httpClient = httpClient;
        }

        protected override async ValueTask OnMessageUpdated(MessageUpdatedEventArgs eventArgs)
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

            if (guildConfig.MessageLogChannelId == default)
            {
                return;
            }
            using (var scope = ServiceProvider.CreateScope())
            {
                var msgHandler = scope.ServiceProvider.GetRequiredService<MessageCreateHandler>();
                if (!msgHandler.GuildProcessMessages.TryGetValue(eventArgs.GuildId.Value, out _))
                {
                    if (!guildConfig.AutomodEnabled)
                    {
                        msgHandler.GuildProcessMessages.Add(eventArgs.GuildId.Value, false);
                    }
                    else
                    {
                        msgHandler.GuildProcessMessages.Add(eventArgs.GuildId.Value, true);
                    }
                }

                if (msgHandler.GuildProcessMessages[eventArgs.GuildId.Value])
                {
                    if (!eventArgs.NewMessage.Author.IsBot)
                    {
                        await msgHandler.AutoModerateAsync(eventArgs.NewMessage, guildConfig);
                    }
                }
            }

            await LogUpdateAsync(eventArgs.OldMessage, eventArgs.NewMessage, guildConfig);
        }

        private async Task LogUpdateAsync(IUserMessage oldMessage, IUserMessage newMessage, GuildConfiguration guildConfiguration)
        {
            if (oldMessage.Content == newMessage.Content) return;
            var channel = await oldMessage.FetchChannelAsync();
            var builder = new StringBuilder()
                .AppendLine($"**{oldMessage.Author.Tag}**(`{oldMessage.Author.Id}`) updated their message in {Mention.Channel(oldMessage.ChannelId)}(#{channel.Name}, `{oldMessage.ChannelId}`)")
                .AppendLine($"**Before:**```{oldMessage.Content}```**After:**```{newMessage.Content}```")
                .ToString();
            await Bot.SendMessageAsync(guildConfiguration.MessageLogChannelId, new LocalMessage()
                .WithContent(builder));
        }
    }
}