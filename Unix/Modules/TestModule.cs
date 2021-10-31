using System.Threading.Tasks;
using Disqord.Bot;
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
    }
}