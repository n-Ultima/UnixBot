using System.Threading.Tasks;
using Disqord.Bot;
using Disqord.Gateway;
using Qmmands;
using Unix.Modules.Attributes;
using Unix.Modules.Bases;
using Unix.Services.Core;

namespace Unix.Modules
{
    public class TestModule : UnixGuildModuleBase
    {
        [Command("ping")]
        [RequireGuildModerator]
        public DiscordCommandResult Ping()
            => Response("Pong");

        [Command("guild-count")]
        [RequireBotOwner]
        public DiscordCommandResult GuildCount()
            => Response($"{Context.Bot.GetGuilds().Count} guilds.");
    }
}