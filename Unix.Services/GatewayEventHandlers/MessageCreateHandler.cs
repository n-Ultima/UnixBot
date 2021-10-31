using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Newtonsoft.Json;
using Unix.Data.Models.Core;
using Unix.Data.Models.Moderation;
using Unix.Services.Core;

namespace Unix.Services.GatewayEventHandlers
{
    public class MessageCreateHandler : UnixService
    {
        private readonly GuildService _guildService;
        private readonly ModerationService _moderationService;
        private readonly HttpClient _httpClient;
        public Dictionary<Snowflake, bool> GuildProcessMessages = new();
        public MessageCreateHandler(HttpClient httpClient, IServiceProvider serviceProvider, GuildService guildService, ModerationService moderationService) : base(serviceProvider)
        {
            _moderationService = moderationService;
            _guildService = guildService;
            _httpClient = httpClient;
        }

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
        {
            if (e.Message is not IUserMessage)
            {
                return;
            }
            if (!e.GuildId.HasValue)
            {
                return;
            }

            if (!OwnerService.WhitelistedGuilds.Contains(e.GuildId.Value))
            {
                return;
            }
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(e.GuildId.Value);
            if (!GuildProcessMessages.TryGetValue(e.GuildId.Value, out var value))
            {
                if (!guildConfig.AutomodEnabled)
                {
                    GuildProcessMessages.Add(e.GuildId.Value, false);
                }
                else
                {
                    GuildProcessMessages.Add(e.GuildId.Value, true);
                }
            }

            if (!GuildProcessMessages[e.GuildId.Value])
            {
                return;
            }

            if (e.Member.RoleIds.Contains(guildConfig.AdministratorRoleId) || e.Member.RoleIds.Contains(guildConfig.ModeratorRoleId))
            {
                return;
            }
            if (e.Message.Content.ToLower().Split(" ").Intersect(guildConfig.BannedTerms).Any())
            {
                await e.Message.DeleteAsync();
                await _moderationService.CreateInfractionAsync(e.GuildId.Value, Bot.CurrentUser.Id, e.Message.Author.Id, InfractionType.Warn, "Message sent contained banned terms.", null);
                return;
            }
            var match = Regex.Match(e.Message.Content, @"(https?://)?(www.)?(discord.(gg|com|io|me|li)|discordapp.com/invite)/([a-z]+)");
            if (match.Success)
            {
                if (await IsGuildWhitelisted(guildConfig, match.Groups[5].ToString()))
                {
                    return;
                }
                await e.Message.DeleteAsync();
                await _moderationService.CreateInfractionAsync(e.GuildId.Value, Bot.CurrentUser.Id, e.Message.Author.Id, InfractionType.Warn, "Message contained invite link not present on whitelist.", null);
                return;
            }
        }

        private async Task<bool> IsGuildWhitelisted(GuildConfiguration guildConfiguration, string code)
        {
            var url = $"https://www.discord.com/api/invites/{code}";
            var result = await _httpClient.GetStringAsync(url);
            var deserializedResult = JsonConvert.DeserializeObject<Root>(result);
            var snowflake = new Snowflake(deserializedResult.Guild.id);
            return guildConfiguration.WhitelistedInvites.Contains(snowflake);
        }
    }

    internal class Root
    {
        public GuildModel Guild { get; set; }
    }
    internal class GuildModel
    {
        public ulong id { get; set; }
    }
    
}