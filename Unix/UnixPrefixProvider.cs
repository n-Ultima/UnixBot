using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Services.Core;

namespace Unix
{
    public class UnixPrefixProvider : IPrefixProvider
    {
        private readonly GuildService _guildService;

        public UnixPrefixProvider(GuildService guildService)
            => _guildService = guildService;
        public async ValueTask<IEnumerable<IPrefix>> GetPrefixesAsync(IGatewayUserMessage message)
        {
            var botClient = message.GetRestClient();
            var guild = await botClient.FetchGuildAsync(message.GuildId.Value);
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(guild.Id);
            if (guildConfig == null)
            {
                return new List<IPrefix> {new StringPrefix("u!"), new MentionPrefix(message.GetGatewayClient().CurrentUser.Id)};
            }
            return await _guildService.FetchIPrefixesAsync(guild.Id);
        } 
    }
}