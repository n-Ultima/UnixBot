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

        [Command("guild-config")]
        public async Task<DiscordCommandResult> GetGuildConfigAsync(Snowflake guildId)
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(guildId);
            if (guildConfig == null)
            {
                return Failure("That guild doesn't have a configuration setup.");
            }

            var guild = await Bot.FetchGuildAsync(guildConfig.Id);
            var ms = new MemoryStream();
            var utf8 = new UTF8Encoding(true);
            var info = utf8.GetBytes(guildConfig.BannedTerms.Humanize());
            ms.Write(info, 0, info.Length);
            ms.Position = 0;
            var ms2 = new MemoryStream();
            var info2 = utf8.GetBytes(guildConfig.WhitelistedInvites.Humanize());
            ms2.Write(info2, 0, info2.Length);
            ms2.Position = 0;
            var embed = new LocalEmbed()
                .WithFields(new[]
                { 
                    new LocalEmbedField().WithName("Prefix").WithValue(guildConfig.Prefix),
                    new LocalEmbedField().WithName("Moderator Role Id").WithValue(guildConfig.ModeratorRoleId.ToString()),
                    new LocalEmbedField().WithName("Administrator Role Id").WithValue(guildConfig.AdministratorRoleId.ToString()),
                    new LocalEmbedField().WithName("Mute Role Id").WithValue(guildConfig.MuteRoleId.ToString()),
                    new LocalEmbedField().WithName("Moderator most Log Channel Id").WithValue(guildConfig.ModLogChannelId.ToString()),
                    new LocalEmbedField().WithName("Message Log Channel Id").WithValue(guildConfig.MessageLogChannelId.ToString())
                })
                .WithTitle($"Guild Configuration for {guild.Name}")
                .WithColor(Color.Gold);
            return Response(new LocalMessage()
                .WithEmbeds(embed)
                .WithAttachments(new LocalAttachment(ms, "bannedTerms.txt"), new LocalAttachment(ms2, "whitelistedinvites.txt")));
        }
    }
}