using System;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Data.Models.Core;
using Unix.Services.Core;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers
{
    public class MessageDeleteHandler : UnixService
    {
        private readonly IGuildService _guildService;

        public MessageDeleteHandler(IServiceProvider serviceProvider, IGuildService guildService) : base(serviceProvider)
        {
            _guildService = guildService;
        }

        protected override async ValueTask OnMessageDeleted(MessageDeletedEventArgs eventArgs)
        {
            var config = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId.Value);
            if (config == null)
            {
                return;
            }

            if (config.MessageLogChannelId == default)
            {
                return;
            }

            if (eventArgs.Message is not IUserMessage message) return;
            await LogMessageDeletionAsync(message, config);
        }

        private async Task LogMessageDeletionAsync(IUserMessage message, GuildConfiguration guildConfiguration)
        {
            var channel = await message.FetchChannelAsync();
            var builder = new StringBuilder()
                .AppendLine($"**{message.Author.Tag}**(`{message.Author.Id}`)'s  message was deleted in {Mention.Channel(channel.Id)}(#{channel.Name}, `{channel.Id}`)")
                .AppendLine($"**Message Content:**```{message.Content}```")
                .ToString();
            await Bot.SendMessageAsync(guildConfiguration.MessageLogChannelId, new LocalMessage()
                .WithContent(builder));
        }
    }
}