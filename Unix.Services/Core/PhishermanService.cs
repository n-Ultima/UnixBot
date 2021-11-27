using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Disqord;
using Disqord.Http;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Unix.Services.Core;

public class PhishermanService : UnixService
{
    private readonly GuildService _guildService;
    private const string PhishermanAPIUri = "https://api.phisherman.gg/v1/domains";
    private readonly HttpClient _httpClient;
    public PhishermanService(GuildService guildService, HttpClient httpClient, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _httpClient = httpClient;
        _guildService = guildService;
    }

    public async Task<bool> IsDomainSuspiciousAsync(Snowflake guildId, string domain)
    {
        var guildConfig = await _guildService.FetchGuildConfigurationAsync(guildId);
        if (guildConfig == null)
        {
            throw new Exception("Not configured.");
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ArgumentNullException(nameof(domain), "Domain must be provided.");
        }
        using (var request = new HttpRequestMessage(HttpMethod.Get, PhishermanAPIUri + $"/{domain}"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue($"Bearer", guildConfig.PhishermanApiKey);
            request.Headers.TryAddWithoutValidation("Content-Type", new[] {"application/json"});
            var responseMessage = await _httpClient.SendAsync(request);
            var stringResponseMessage = await responseMessage.Content.ReadAsStringAsync();
            var boolRes = Convert.ToBoolean(stringResponseMessage);
            return boolRes;
        }
    }

    
    public async Task<bool> IsVerifiedPhishAsync(Snowflake guildId, string domain)
    {
        var guildConfig = await _guildService.FetchGuildConfigurationAsync(guildId);
        if (guildConfig == null)
        {
            throw new Exception("Not configured.");
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ArgumentNullException(nameof(domain), "Domain must be provided.");
        }
        using (var request = new HttpRequestMessage(HttpMethod.Get, PhishermanAPIUri + $"/info/{domain}"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue($"Bearer", guildConfig.PhishermanApiKey);
            request.Headers.TryAddWithoutValidation("Content-Type", new[] {"application/json"});
            var responseMessage = await _httpClient.SendAsync(request);
            var stringResponseMessage = await responseMessage.Content.ReadAsStringAsync();
            JObject jObject = JObject.Parse(stringResponseMessage);
            bool isVerified = Convert.ToBoolean(jObject[domain]["verifiedPhish"].ToString());
            return isVerified;
        }
    }
    
    public async Task ReportCaughtPhishAsync(Snowflake guildId, string domain)
    {
        var guildConfig = await _guildService.FetchGuildConfigurationAsync(guildId);
        if (guildConfig == null)
        {
            throw new Exception("Not configured.");
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ArgumentNullException(nameof(domain), "Domain must be provided.");
        }
        using (var request = new HttpRequestMessage(HttpMethod.Put, PhishermanAPIUri + $"/{domain}"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue($"Bearer", guildConfig.PhishermanApiKey);
            request.Headers.TryAddWithoutValidation("Content-Type", new[] {"application/json"});
            request.Headers.Add("guild", new[] {guildId.ToString()});
            await _httpClient.SendAsync(request);
        }
    }
}