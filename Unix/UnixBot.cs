using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Sharding;
using Disqord.Rest;
using Disqord.Sharding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using Serilog;
using Unix.Common;
using Unix.Services.Core;

namespace Unix
{
    public class UnixBot : DiscordBotSharder
    {
        private readonly GuildService _guildService;
        public UnixBot(GuildService guildService, IOptions<DiscordBotSharderConfiguration> options, ILogger<DiscordBotSharder> logger, IServiceProvider services, DiscordClientSharder client) : base(options, logger, services, client)
        {
            _guildService = guildService;
        }

    }
}