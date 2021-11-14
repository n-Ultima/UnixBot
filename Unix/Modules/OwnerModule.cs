using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Humanizer;
using Qmmands;
using Unix.Modules.Bases;
using Unix.Services;
using Unix.Services.Core;

namespace Unix.Modules
{
    [RequireBotOwner]
    public class OwnerModule : UnixGuildModuleBase
    {
        private readonly OwnerService _ownerService;
        private readonly GuildService _guildService;
        public OwnerModule(OwnerService ownerService, GuildService guildService)
        {
            _ownerService = ownerService;
            _guildService = guildService;
        }
        [Command("configure-guild")]
        [Description("Configures a guild for use with Unix.")]
        public async Task<DiscordCommandResult> ConfigureGuildAsync(Snowflake guildId, string prefix, Snowflake muteRoleId, Snowflake modLogChannelId, Snowflake messsageLogChannelId, Snowflake modRoleId, Snowflake adminRoleId, bool automodEnabled)
        {
            await _ownerService.ConfigureGuildAsync(guildId, prefix, muteRoleId, modLogChannelId, messsageLogChannelId,  modRoleId, adminRoleId, automodEnabled);
            return Success("Guild configured successfully.");
        }

        [Command("disallow-guild", "blacklist-guild")]
        [Description("Blacklists a guild.")]
        public async Task<DiscordCommandResult> DisallowGuildAsync(Snowflake guildId)
        {
            await _ownerService.BlacklistGuildAsync(guildId);
            var guild = await Bot.FetchGuildAsync(guildId);
            return Success($"Guild **{guild.Name}** has been blacklisted.");
        }
    }
}