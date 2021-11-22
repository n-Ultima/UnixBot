using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
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

        [Command("slash")]
        [RequireBotOwner]
        public async Task<DiscordCommandResult> Slash()
        {
            var pingCmd = new LocalSlashCommand()
                .WithName("ping")
                .WithDescription("Pings the Discord API and returns the latency.");
            var configMuteRole = new LocalSlashCommand()
                .WithName("configure-muterole")
                .WithDescription("Sets the mute role for your server.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("role")
                        .WithDescription("The role to set.")
                        .WithType(SlashCommandOptionType.Role)
                });
            await Bot.CreateGuildApplicationCommandAsync(Context.Bot.CurrentUser.Id, Context.GuildId, pingCmd);
            await Bot.CreateGuildApplicationCommandAsync(Context.Bot.CurrentUser.Id, Context.GuildId, configMuteRole);
            return Success("Did it work");
        }
    }
}