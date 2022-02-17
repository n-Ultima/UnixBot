using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Unix.Common;
using Unix.Data;
using Unix.Data.Models.Core;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.CoreEventHandlers;

public class GuildJoinedHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly ILogger<GuildJoinedHandler> _logger;
    private readonly UnixConfiguration UnixConfig = new();

    public GuildJoinedHandler(IGuildService guildService, ILogger<GuildJoinedHandler> logger, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _guildService = guildService;
        _logger = logger;
    }

    protected override async ValueTask OnJoinedGuild(JoinedGuildEventArgs e)
    {
        var guildConfig = await _guildService.FetchGuildConfigurationAsync(e.GuildId);
        if (guildConfig == null)
        {
            if (UnixConfig.PrivelegedMode)
            {
                return;
            }

            var firstChannel = e.Guild.Channels
                .Select(x => x.Value)
                .Where(x => x.Type == ChannelType.Text)
                .First();
            try
            {
                await (firstChannel as ITextChannel).SendMessageAsync(new LocalMessage()
                    .WithContent("Thank you for adding Unix! A few things to note:\n1. Join the Unix Discord Server for support and bug issues(http://www.ultima.one/unix)\n2. If this guild is found to be in violation of Discord's TOS or any other applicable laws, the guild will be blacklisted from using Unix.\n3. Please run commands to setup your guild fully! All of these commands start with the `/configure` prefix, such as `/configure-modrole`."));
            }
            catch (RestApiException)
            {
                try
                {
                    await Bot.SendMessageAsync(e.Guild.OwnerId, new LocalMessage()
                        .WithContent("Thank you for adding Unix! A few things to note:\n1. Join the Unix Discord Server for support and bug issues(http://www.ultima.one/unix)\n2. If this guild is found to be in violation of Discord's TOS or any other applicable laws, the guild will be blacklisted from using Unix.\n3. Please run commands to setup your guild fully! All of these commands start with the `/configure` prefix, such as `/configure-modrole`."));
                }
                catch (RestApiException)
                {
                    _logger.LogInformation("Unable to send welcome message for guild {gId}", e.GuildId);
                }
            }

            _logger.LogInformation("Joined guild {guild} while in non-priveleged mode, creating guild configuration...", e.Guild.Name);
            await _guildService.CreateGuildConfigurationAsync(e.GuildId);
            _logger.LogInformation("Config created!");
        }
    }
}