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
using Serilog;
using Unix.Data.Models.Core;
using Unix.Data.Models.Moderation;
using Unix.Services.Core;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers
{
    public class MessageCreateHandler : UnixService
    {
        private readonly IGuildService _guildService;
        private readonly IModerationService _moderationService;
        private readonly HttpClient _httpClient;
        private readonly IPhishermanService _phishermanService;
        public Dictionary<Snowflake, bool> GuildProcessMessages = new();
        public MessageCreateHandler(HttpClient httpClient, IServiceProvider serviceProvider, IGuildService guildService, IModerationService moderationService, IPhishermanService phishermanService) : base(serviceProvider)
        {
            _moderationService = moderationService;
            _guildService = guildService;
            _httpClient = httpClient;
            _phishermanService = phishermanService;
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

            if (eventArgs.Member == null)
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

            if (eventArgs.Member.RoleIds.Contains(guildConfig.AdministratorRoleId) || eventArgs.Member.RoleIds.Contains(guildConfig.ModeratorRoleId) || eventArgs.Member.Id == eventArgs.Member.GetGuild().OwnerId)
            {
                return;
            }

            if (eventArgs.Member.IsBot)
            {
                return;
            }
            if (eventArgs.Message.Content.ToLower().Split(" ").Intersect(guildConfig.BannedTerms).Any())
            {
                await eventArgs.Message.DeleteAsync();
                await _moderationService.CreateInfractionAsync(eventArgs.GuildId.Value, Bot.CurrentUser.Id, eventArgs.Message.Author.Id, InfractionType.Warn, "Message sent contained banned terms.", false, null);
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
                await _moderationService.CreateInfractionAsync(eventArgs.GuildId.Value, Bot.CurrentUser.Id, eventArgs.Message.Author.Id, InfractionType.Warn, "Message contained invite link not present on whitelist.", false, null);
                return;
            }

            if (guildConfig.PhishermanApiKey != null)
            {
                var linkMatch = Regex.Match(eventArgs.Message.Content, @"(https?://)?(www\.)?([a-z]+)\.([a-z]+)");
                if (linkMatch.Success)
                {
                    var group = linkMatch.Groups[0].ToString();
                    var isSus = await _phishermanService.IsDomainSuspiciousAsync(eventArgs.GuildId.Value, group);
                    if (isSus)
                    {
                        // The domain is marked as suspicious. We now need to actually see if it's a verified phish.
                        var isVerifiedPhish = await _phishermanService.IsVerifiedPhishAsync(eventArgs.GuildId.Value, group);
                        if (isVerifiedPhish)
                        {
                            // delete the message, report back to the API.
                            await eventArgs.Message.DeleteAsync();
                            await _moderationService.CreateInfractionAsync(eventArgs.GuildId.Value, Bot.CurrentUser.Id, eventArgs.Message.Author.Id, InfractionType.Warn, $"Message sent contained a suspicious link({group})", false, null);
                            await _phishermanService.ReportCaughtPhishAsync(eventArgs.GuildId.Value, group);
                            return;
                        }
                    }
                }
            }
        }

        public async Task AutoModerateAsync(IUserMessage message, GuildConfiguration guildConfig)
        {
            if (message.Content.ToLower().Split(" ").Intersect(guildConfig.BannedTerms).Any())
            {
                await message.DeleteAsync();
                await _moderationService.CreateInfractionAsync(guildConfig.Id, Bot.CurrentUser.Id, message.Author.Id, InfractionType.Warn, "Message sent contained banned terms.", false, null);
            }
            var match = Regex.Match(message.Content, @"(https?://)?(www.)?(discord.(gg|com|io|me|li)|discordapp.com/invite)/([a-z]+)");
            if (match.Success)
            {
                if (await IsGuildWhitelisted(guildConfig, match.Groups[5].ToString()))
                {
                    return;
                }
                await message.DeleteAsync();
                await _moderationService.CreateInfractionAsync(guildConfig.Id, Bot.CurrentUser.Id, message.Author.Id, InfractionType.Warn, "Message contained invite link not present on whitelist.", false, null);
            }
            if (guildConfig.PhishermanApiKey != null)
            {
                var linkMatch = Regex.Match(message.Content, @"(https?://)?(www\.)?([a-z]+)\.([a-z]+)");
                if (linkMatch.Success)
                {
                    var group = linkMatch.Groups[0].ToString();
                    var isSus = await _phishermanService.IsDomainSuspiciousAsync(guildConfig.Id, group);
                    if (isSus)
                    {
                        // The domain is marked as suspicious. We now need to actually see if it's a verified phish.
                        var isVerifiedPhish = await _phishermanService.IsVerifiedPhishAsync(guildConfig.Id, group);
                        if (isVerifiedPhish)
                        {
                            // delete the message, report back to the API.
                            await message.DeleteAsync();
                            await _moderationService.CreateInfractionAsync(guildConfig.Id, Bot.CurrentUser.Id, message.Author.Id, InfractionType.Warn, $"Message sent contained a suspicious link({group})", false, null);
                            await _phishermanService.ReportCaughtPhishAsync(guildConfig.Id, group);
                            return;
                        }
                    }
                }
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