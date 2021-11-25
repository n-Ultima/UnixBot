using System;
using System.Collections.Generic;
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
            List<LocalSlashCommand> cmds = new(); 
            var pingCmd = new LocalSlashCommand()
                .WithName("ping")
                .WithDescription("Pings the Discord API and returns the latency.");
            cmds.Add(pingCmd);
            var configMuteRole = new LocalSlashCommand()
                .WithName("configure-muterole")
                .WithDescription("Sets the mute role for your server.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("role")
                        .WithDescription("The role to set.")
                        .WithType(SlashCommandOptionType.Role)
                        .WithIsRequired()
                });
            cmds.Add(configMuteRole);
            var guildCountCmd = new LocalSlashCommand()
                .WithName("guild-count")
                .WithDescription("Returns the number of guilds that this instance of Unix is in.");
            cmds.Add(guildCountCmd);
            var configModRoleCmd = new LocalSlashCommand()
                .WithName("configure-modrole")
                .WithDescription("Configures the moderator role for your server.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("role")
                        .WithDescription("The role to set.")
                        .WithType(SlashCommandOptionType.Role)
                        .WithIsRequired()
                });
            cmds.Add(configModRoleCmd);
            var configAdminRoleCmd = new LocalSlashCommand()
                .WithName("configure-adminrole")
                .WithDescription("Configures the administrator role for your server.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("role")
                        .WithDescription("The role to set.")
                        .WithType(SlashCommandOptionType.Role)
                        .WithIsRequired()
                });
            cmds.Add(configAdminRoleCmd);
            var configModLogCmd = new LocalSlashCommand()
                .WithName("configure-modlog")
                .WithDescription("Configures the channel that moderation actions performed with Unix will be logged to.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("channel")
                        .WithDescription("The channel to log events to.")
                        .WithType(SlashCommandOptionType.Channel)
                        .WithIsRequired()

                });
            cmds.Add(configModLogCmd);
            var configMessageLogCmd = new LocalSlashCommand()
                .WithName("configure-messagelog")
                .WithDescription("Configures the channel that message edits and deletions will be logged to.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("channel")
                        .WithDescription("The channel to log events to.")
                        .WithType(SlashCommandOptionType.Channel)
                        .WithIsRequired()

                });
            cmds.Add(configMessageLogCmd);
            var configAutomodCmd = new LocalSlashCommand()
                .WithName("configure-automod")
                .WithDescription("Configures wheter Unix should automoderate your server.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("enabled")
                        .WithDescription("Whether automod is enabled.")
                        .WithType(SlashCommandOptionType.Boolean)
                        .WithIsRequired()

                });
            cmds.Add(configAutomodCmd);
            var configSpamCmd = new LocalSlashCommand()
                .WithName("configure-spam")
                .WithDescription("Configures the amount of messages a user must send in 3 seconds to be warned.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("amount")
                        .WithDescription("The amount of messages.")
                        .WithType(SlashCommandOptionType.Number)
                        .WithIsRequired()
                });
            cmds.Add(configSpamCmd);
            var addBannedTermCmd = new LocalSlashCommand()
                .WithName("add-banned-term")
                .WithDescription("Adds the provided term to the blacklist for your server, warning members for sending it.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("term")
                        .WithDescription("The term to add to the list.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired()
                });
            cmds.Add(addBannedTermCmd);
            var removeBannedTermCmd = new LocalSlashCommand()
                .WithName("remove-banned-term")
                .WithDescription("Removes the banned term provided from the list of banned terms in your server.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("term")
                        .WithDescription("The term to remove from the list.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired()
                });
            cmds.Add(removeBannedTermCmd);
            var addWhitelistedGuildCmd = new LocalSlashCommand()
                .WithName("add-whitelisted-guild")
                .WithDescription("Adds a guild to the whitelist, allowing invites pointing towards it to pass through the automod.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("id")
                        .WithDescription("The ID of the guild.")
                        .WithIsRequired()
                        .WithType(SlashCommandOptionType.String)
                });
            cmds.Add(addWhitelistedGuildCmd);
            var removeWhitelistedGuildCmd = new LocalSlashCommand()
                .WithName("remove-whitelisted-guild")
                .WithDescription("Adds a guild to the whitelist, allowing invites pointing towards it to pass through the automod.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("id")
                        .WithDescription("The ID of the guild.")
                        .WithIsRequired()
                        .WithType(SlashCommandOptionType.String)
                });
            cmds.Add(removeWhitelistedGuildCmd);
            var configureGuildCmd = new LocalSlashCommand()
                .WithName("configure-guild")
                .WithDescription("Configures a guild for use with Unix.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("id")
                        .WithDescription("The ID of the guild.")
                        .WithIsRequired()
                        .WithType(SlashCommandOptionType.String),
                    new LocalSlashCommandOption()
                        .WithName("mute-role-id")
                        .WithDescription("The ID of the muted role.")
                        .WithIsRequired()
                        .WithType(SlashCommandOptionType.String),
                    new LocalSlashCommandOption()
                        .WithName("modlog-channel-id")
                        .WithDescription("The ID of the moderation log channel.")
                        .WithIsRequired()
                        .WithType(SlashCommandOptionType.String),
                    new LocalSlashCommandOption()
                        .WithName("messagelog-channel-id")
                        .WithDescription("The ID of the message log channel.")
                        .WithIsRequired()
                        .WithType(SlashCommandOptionType.String),
                    new LocalSlashCommandOption()
                        .WithName("moderator-role-id")
                        .WithDescription("The ID of the moderator role.")
                        .WithIsRequired()
                        .WithType(SlashCommandOptionType.String),
                    new LocalSlashCommandOption()
                        .WithName("administrator-role-id")
                        .WithDescription("The ID of the administrator role.")
                        .WithIsRequired()
                        .WithType(SlashCommandOptionType.String),
                    new LocalSlashCommandOption()
                        .WithName("automod-enabled")
                        .WithDescription("If automod should be enabled.")
                        .WithIsRequired()
                        .WithType(SlashCommandOptionType.Boolean)
                });
            cmds.Add(configureGuildCmd);
            var disallowGuildCmd = new LocalSlashCommand()
                .WithName("disallow-gulid")
                .WithDescription("Disallows a guild from using Unix.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("id")
                        .WithDescription("The guild's ID.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired()
                });
            cmds.Add(disallowGuildCmd);
            var infractionsCmd = new LocalSlashCommand()
                .WithName("infractions")
                .WithDescription("Fetches the infractions for the user provided.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("user")
                        .WithDescription("The user whose infractions to fetch.")
                        .WithType(SlashCommandOptionType.User)
                        .WithIsRequired()
                });
            cmds.Add(infractionsCmd);
            var infractionCmd = new LocalSlashCommand()
                .WithName("infraction")
                .WithDescription("Fetches the infraction info for the GUID provided.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("id")
                        .WithDescription("The ID of the infraction.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired()
                });
            cmds.Add(infractionCmd);
            var updateInfractionCmd = new LocalSlashCommand()
                .WithName("infraction-update")
                .WithDescription("Updates the reason for the infraction provided.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("id")
                        .WithDescription("The ID of the infraction.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired(),
                    new LocalSlashCommandOption()
                        .WithName("reason")
                        .WithDescription("The new reason that should be applied to the infraction.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired()
                });
            cmds.Add(updateInfractionCmd);
            var deleteInfractionCmd = new LocalSlashCommand()
                .WithName("infraction-delete")
                .WithDescription("Deletes the infraction provided.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("id")
                        .WithDescription("The ID of the infraction.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired(),
                    new LocalSlashCommandOption()
                        .WithName("reason")
                        .WithDescription("The reason for deleting this infraction.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired()
                });
            cmds.Add(deleteInfractionCmd);
            var banCmd = new LocalSlashCommand()
                .WithName("ban")
                .WithDescription("Bans the user provided.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("user")
                        .WithDescription("The user to ban.")
                        .WithType(SlashCommandOptionType.User)
                        .WithIsRequired(),
                    new LocalSlashCommandOption()
                        .WithName("reason")
                        .WithDescription("The reason for the ban.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired(),
                    new LocalSlashCommandOption()
                        .WithName("duration")
                        .WithDescription("The optional duration of the ban.")
                        .WithType(SlashCommandOptionType.String)
                });
            cmds.Add(banCmd);
            var muteCmd = new LocalSlashCommand()
                .WithName("mute")
                .WithDescription("Mutes the user provided.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("user")
                        .WithDescription("The member to mute.")
                        .WithType(SlashCommandOptionType.User)
                        .WithIsRequired(),
                    new LocalSlashCommandOption()
                        .WithName("reason")
                        .WithDescription("The reason for the mute.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired(),
                    new LocalSlashCommandOption()
                        .WithName("duration")
                        .WithDescription("The duration of the mute.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired()
                });
            cmds.Add(muteCmd);
            var noteCmd = new LocalSlashCommand()
                .WithName("note")
                .WithDescription("Records a note for the user provided.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("user")
                        .WithDescription("The member to record the note for.")
                        .WithType(SlashCommandOptionType.User)
                        .WithIsRequired(),
                    new LocalSlashCommandOption()
                        .WithName("reason")
                        .WithDescription("The content of the note.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired()
                });
            cmds.Add(noteCmd);
            var warnCmd = new LocalSlashCommand()
                .WithName("warn")
                .WithDescription("Warns a user")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("user")
                        .WithDescription("The member to warn.")
                        .WithType(SlashCommandOptionType.User)
                        .WithIsRequired(),
                    new LocalSlashCommandOption()
                        .WithName("reason")
                        .WithDescription("The reason for the warn.")
                        .WithType(SlashCommandOptionType.String)
                        .WithIsRequired()
                });
            cmds.Add(warnCmd);
            var purgeCmd = new LocalSlashCommand()
                .WithName("purge")
                .WithDescription("Purges messages.")
                .WithOptions(new[]
                {
                    new LocalSlashCommandOption()
                        .WithName("count")
                        .WithDescription("The amount of messages to purge.")
                        .WithType(SlashCommandOptionType.Integer)
                        .WithIsRequired(),
                    new LocalSlashCommandOption()
                        .WithName("user")
                        .WithDescription("The optional member who's messages to clear.")
                        .WithType(SlashCommandOptionType.User)
                });
            cmds.Add(purgeCmd);
            await Bot.SetGuildApplicationCommandsAsync(Context.Bot.CurrentUser.Id, Context.GuildId, cmds);
            await Bot.SetGuildApplicationCommandsAsync(Context.Bot.CurrentUser.Id, 826243808710098954, cmds);

            return Success("Did it work");
        }
    }
}