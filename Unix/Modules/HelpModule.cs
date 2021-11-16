using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Qmmands;
using Unix.Modules.Attributes;
using Unix.Modules.Bases;
using Unix.Services.Core;

namespace Unix.Modules
{
    [Group("help")]
    public class HelpModule : UnixGuildModuleBase
    {
        private readonly GuildService _guildService;
        private readonly CommandService _commandService;
        
        public HelpModule(GuildService guildService, CommandService commandService)
        {
            _guildService = guildService;
            _commandService = commandService;
        }

        [Command("")]
        [Description("Provides help for the command provided.")]
        public async Task<DiscordCommandResult> HelpAsync(
            [Description("The command that help should be displayed for.")]
                string commandName)
        {
            var commandMatches = _commandService.FindCommands(commandName);
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(Context.GuildId);
            if (commandMatches.Count == 1)
            {
                var command = commandMatches[0].Command;
                var builder = new StringBuilder();
                builder.AppendLine($"**{guildConfig.Prefix}{command.Name}**");
                builder.AppendLine(command.Description);
                builder.AppendLine($"Parameters: `{string.Join(" ", command.Parameters)}`");

                return Response(builder.ToString());
            }
            else
            {
                var builder = new StringBuilder();
                builder.AppendLine("**Matches Found:**");
                foreach(var cmd in commandMatches)
                {
                    var command = cmd.Command;
                    builder.AppendLine($"**{guildConfig.Prefix}{command.Name}**");
                    builder.AppendLine(command.Description);
                    builder.AppendLine($"Parameters: `{string.Join(" ", command.Parameters)}`");
                    builder.AppendLine();
                }

                return Response(builder.ToString());
            }
        }
    }
}