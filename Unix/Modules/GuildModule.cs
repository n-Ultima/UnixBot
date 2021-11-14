using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Qmmands;
using Unix.Data;
using Unix.Modules.Attributes;
using Unix.Modules.Bases;
using Unix.Services.Core;

namespace Unix.Modules
{
    [Name("GuildConfig")]
    [Description("Provides commands used for configuring a guild for Unix use.")]
    [RequireGuildOwner]
    public class GuildModule : UnixGuildModuleBase
    {
        private readonly GuildService _guildService;

        private readonly IServiceProvider ServiceProvider; // Until i finish everything nicely

        public GuildModule(GuildService guildService, IServiceProvider serviceProvider)
        {
            _guildService = guildService;
            ServiceProvider = serviceProvider;
        }

        [Command("configure-prefix")]
        [RequireGuildAdministrator]
        [Description("Changes your prefix.")]
        public async Task<DiscordCommandResult> ConfigureGuildPrefixAsync([Remainder] string newPrefix)
        {
            var oldPrefix = await _guildService.FetchGuildPrefixAsync(Context.GuildId);
            await _guildService.ModifyGuildPrefixAsync(Context.GuildId, newPrefix);
            return Success($"Prefix modified from `{oldPrefix}` to `{newPrefix}`");
        }

        [Command("configure-muterole")]
        [RequireGuildAdministrator]
        [Description("Changes your guild's mute role.")]
        public async Task<DiscordCommandResult> ConfigureGuildMuteRoleAsync(IRole role)
        {
            await _guildService.ModifyGuildMuteRoleIdAsync(Context.GuildId, role.Id);
            return Success($"Muterole has now been set to **{role.Name}**");
        }

        [Command("configure-modlog")]
        [RequireGuildAdministrator]
        [Description("Changes your guild's mod log channel.")]
        public async Task<DiscordCommandResult> ConfigureGuildModlogChannelAsync(ITextChannel channel)
        {
            await _guildService.ModifyGuildModLogChannelIdAsync(Context.GuildId, channel.Id);
            return Success($"Moderation actions will now be logged to {Mention.Channel(channel)}");
        }

        [Command("configure-messagelog")]
        [RequireGuildAdministrator]
        [Description("Changes your guild's message log channel")]
        public async Task<DiscordCommandResult> ConfigureGuildMessageLogChannelAsync(ITextChannel channel)
        {
            await _guildService.ModifyGuildMessageLogChannelIdAsync(Context.GuildId, channel.Id);
            return Success($"Message edits and deletions will now be logged to {Mention.Channel(channel)}");
        }
        [Command("configure-automod")]
        [RequireGuildAdministrator]
        [Description("Enables or disables automoderation for your guild.(True is enabled, false is disabled)")]
        public async Task<DiscordCommandResult> ConfigureAutomdAsync(bool enabled)
        {
            await _guildService.ConfigureGuildAutomodAsync(Context.GuildId, enabled);
            return Success($"Automod status is set to {enabled}");
        }

        [Command("configure-modrole")]
        [RequireGuildAdministrator]
        [Description("Configures the moderator role for your guild.")]
        public async Task<DiscordCommandResult> ConfigureModRoleAsync(IRole modRole)
        {
            await _guildService.ModifyGuildModRoleAsync(Context.GuildId, modRole.Id);
            return Success($"The moderator role is now set to **{modRole.Name}**");
        }
        [Command("configure-adminrole")]
        [RequireGuildAdministrator]
        [Description("Configures the moderator role for your guild.")]
        public async Task<DiscordCommandResult> ConfigureAdminRoleAsync(IRole adminRole)
        {
            await _guildService.ModifyGuildAdminRoleAsync(Context.GuildId, adminRole.Id);
            return Success($"The moderator role is now set to **{adminRole.Name}**");
        }

        [Command("configure-spam")]
        [RequireGuildAdministrator]
        [Description("Configures the amount of messages considered spam in your guild.")]
        public async Task<DiscordCommandResult> ConfigureSpamAmountAsync(int amount)
        {
            await _guildService.ModifyGuildSpamThresholdAsync(Context.GuildId, amount);
            return Success($"If a user sends more than `{amount}` messages in `3` seconds, they will be warned for spam.");
        }

        [Command("add-banned-term")]
        [Description("Adds a banned term to the guilds banned term list.")]
        [RequireGuildAdministrator]
        public async Task<DiscordCommandResult> AddBannedTermAsync(string word)
        {
            await _guildService.AddBannedTermAsync(Context.GuildId, word);
            return Success($"Users that do not have Moderator or Administrator permissions will now be warned for using the term `{word}`");
        }

        [Command("remove-banned-term")]
        [Description("Removes a banned term from the guilds banned term list.")]
        [RequireGuildAdministrator]
        public async Task<DiscordCommandResult> RemoveBannedTermAsync(string word)
        {
            await _guildService.RemoveBannedTermAsync(Context.GuildId, word);
            return Success($"Users will no longer be warned for using `{word}`");
        }

        [Command("add-whitelisted-guild")]
        [Description("Adds a guild to the whitelist, allowing invites pointed towards said guild to pass through the automod.")]
        [RequireGuildAdministrator]
        public async Task<DiscordCommandResult> AddWhitelistedGuildAsync(Snowflake guildId)
        {
            var guild = await Bot.FetchGuildAsync(guildId);
            if (guild == null)
            {
                return Failure("Invalid guild ID.");
            }
            await _guildService.AddWhitelistedInviteAsync(Context.GuildId, guildId);
            return Success($"Invites pointing towards `{guild.Name}` will not be deleted.");
        }
        [Command("remove-whitelisted-guild")]
        [Description("Adds a guild to the whitelist, allowing invites pointed towards said guild to pass through the automod.")]
        [RequireGuildAdministrator]
        public async Task<DiscordCommandResult> RemoveWhitelistedGuildAsync(Snowflake guildId)
        {
            var guild = await Bot.FetchGuildAsync(guildId);
            if (guild == null)
            {
                return Failure("Invalid guild ID.");
            }
            await _guildService.RemoveWhitelistedInviteAsync(Context.GuildId, guildId);
            return Success($"Invites pointing towards `{guild.Name}` will now be deleted.");
        }
    }
}