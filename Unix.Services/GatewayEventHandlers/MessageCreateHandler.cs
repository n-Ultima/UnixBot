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

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Message is not IUserMessage)
            {
                return;
            }
            if (!eventArgs.GuildId.HasValue)
            {
                return;
            }

            if (!OwnerService.WhitelistedGuilds.Contains(eventArgs.GuildId.Value))
            {
                return;
            }
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(eventArgs.GuildId.Value);
            if (!GuildProcessMessages.TryGetValue(eventArgs.GuildId.Value, out var value))
            {
                if (!guildConfig.AutomodEnabled)
                {
                    GuildProcessMessages.Add(eventArgs.GuildId.Value, false);
                }
                else
                {
                    GuildProcessMessages.Add(eventArgs.GuildId.Value, true);
                }
            }

            if (!GuildProcessMessages[eventArgs.GuildId.Value])
            {
                return;
            }

            if (eventArgs.Member.RoleIds.Contains(guildConfig.AdministratorRoleId) || eventArgs.Member.RoleIds.Contains(guildConfig.ModeratorRoleId))
            {
                return;
            }
            if (eventArgs.Message.Content.ToLower().Split(" ").Intersect(guildConfig.BannedTerms).Any())
            {
                await eventArgs.Message.DeleteAsync();
                await _moderationService.CreateInfractionAsync(eventArgs.GuildId.Value, Bot.CurrentUser.Id, eventArgs.Message.Author.Id, InfractionType.Warn, "Message sent contained banned terms.", null);
                return;
            }
            var match = Regex.Match(eventArgs.Message.Content, @"(https?://)?(www.)?(discord.(gg|com|io|me|li)|discordapp.com/invite)/([a-z]+)");
            if (match.Success)
            {
                if (await IsGuildWhitelisted(guildConfig, match.Groups[5].ToString()))
                {
                    return;
                }
                await eventArgs.Message.DeleteAsync();
                await _moderationService.CreateInfractionAsync(eventArgs.GuildId.Value, Bot.CurrentUser.Id, eventArgs.Message.Author.Id, InfractionType.Warn, "Message contained invite link not present on whitelist.", null);
                return;
            }
        }

        public async Task AutoModerateAsync(IUserMessage message, GuildConfiguration guildConfig)
        {
            if (message.Content.ToLower().Split(" ").Intersect(guildConfig.BannedTerms).Any())
            {
                await message.DeleteAsync();
                await _moderationService.CreateInfractionAsync(guildConfig.Id, Bot.CurrentUser.Id, message.Author.Id, InfractionType.Warn, "Message sent contained banned terms.", null);
            }
            var match = Regex.Match(message.Content, @"(https?://)?(www.)?(discord.(gg|com|io|me|li)|discordapp.com/invite)/([a-z]+)");
            if (match.Success)
            {
                if (await IsGuildWhitelisted(guildConfig, match.Groups[5].ToString()))
                {
                    return;
                }
                await message.DeleteAsync();
                await _moderationService.CreateInfractionAsync(guildConfig.Id, Bot.CurrentUser.Id, message.Author.Id, InfractionType.Warn, "Message contained invite link not present on whitelist.", null);
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