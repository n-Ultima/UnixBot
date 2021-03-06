using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Unix.Common;
using Unix.Data;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.GatewayEventHandlers.CoreEventHandlers;

public class ReadyEventHandler : UnixService
{
    private readonly IGuildService _guildService;
    private readonly ILogger<ReadyEventHandler> _logger;
    public UnixConfiguration UnixConfig = new();

    public ReadyEventHandler(IGuildService guildService, ILogger<ReadyEventHandler> logger, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _guildService = guildService;
        _logger = logger;
    }

    protected override async ValueTask OnReady(ReadyEventArgs e)
    {
        await Bot.SetPresenceAsync(UserStatus.Online, new LocalActivity("slash commands", ActivityType.Watching));
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var allowedGuildIds = await unixContext.GuildConfigurations.Select(x => x.Id).ToListAsync();
            var unauthorizedGuilds = e.GuildIds.Except(allowedGuildIds);
            if (unauthorizedGuilds.Any() && UnixConfig.PrivelegedMode)
            {
                _logger.LogWarning("Guilds were found that Unix isn't authorized to operate in. IDs: [{guildIds}]", unauthorizedGuilds.Humanize());
                // Now, we leave each of the guilds that Unix shouldn't be in.
                foreach (var guild in unauthorizedGuilds)
                {
                    var g = await Bot.FetchGuildAsync(guild);
                    await Bot.LeaveGuildAsync(guild, new DefaultRestRequestOptions
                    {
                        Reason = "Unauthorized. Join the Unix server(http://www.ultima.one/unix) to request access."
                    });
                    var gName = g.Name;
                    _logger.LogInformation("Left guild {g} due to lack of authorizaiton.", gName);
                }
            }

            if (!UnixConfig.PrivelegedMode)
            {
                foreach (var guild in e.GuildIds)
                {
                    if (!allowedGuildIds.Contains(guild))
                    {
                        _logger.LogInformation("Guild found that doesn't have a configuration created Creating. ID: {id}", guild);
                        await _guildService.CreateGuildConfigurationAsync(guild);
                        _logger.LogInformation("Created!");
                    }
                }
            }
            var globalCmds = await Bot.FetchGlobalApplicationCommandsAsync(Bot.CurrentUser.Id);
            if (!globalCmds.Any())
            {
                _logger.LogInformation("Setting up global slash commands(takes 1 hour approximately).");
                await SetupUnixGlobalSlashCommandsAsync();
            }
#if DEBUG
            await SetupUnixGlobalSlashCommandsAsync();
#endif
        }
    }
    public async Task SetupUnixGlobalSlashCommandsAsync()
    {
        List<LocalSlashCommand> cmds = new();
        var configCmd = new LocalSlashCommand()
            .WithName("config")
            .WithDescription("Fetches a guild configuration. If no ID is provided, the current guild is used.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("id")
                    .WithDescription("The ID of the guild.")
                    .WithType(SlashCommandOptionType.String)
            });
        cmds.Add(configCmd);
        var pingCmd = new LocalSlashCommand()
            .WithName("ping")
            .WithDescription("Pings the Discord API and returns the latency.");
        cmds.Add(pingCmd);
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
        var configMiscLogCmd = new LocalSlashCommand()
            .WithName("configure-miscellaneouslog")
            .WithDescription("Configures the channel that miscellaneous log events will be logged to.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("channel")
                    .WithDescription("The channel to log events to.")
                    .WithType(SlashCommandOptionType.Channel)
                    .WithIsRequired()
            });
        cmds.Add(configMiscLogCmd);
        var configAutomodCmd = new LocalSlashCommand()
            .WithName("configure-automod")
            .WithDescription("Configures whether Unix should automoderate your server.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("enabled")
                    .WithDescription("Whether automod is enabled.")
                    .WithType(SlashCommandOptionType.Boolean)
                    .WithIsRequired()

            });
        cmds.Add(configAutomodCmd);
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
        var addAutoRoleCmd = new LocalSlashCommand()
            .WithName("add-autorole")
            .WithDescription("Adds the provided role to the guild's list of autoroles.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("role")
                    .WithDescription("The role to add.")
                    .WithType(SlashCommandOptionType.Role)
                    .WithIsRequired()
            });
        cmds.Add(addAutoRoleCmd);
        var removeAutoRoleCmd = new LocalSlashCommand()
            .WithName("remove-autorole")
            .WithDescription("Removes the provided role from the guild's list of autoroles.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("role")
                    .WithDescription("The role to remove.")
                    .WithType(SlashCommandOptionType.Role)
                    .WithIsRequired()
            });
        cmds.Add(removeAutoRoleCmd);
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
        var rescindInfractionCmd = new LocalSlashCommand()
            .WithName("infraction-rescind")
            .WithDescription("Rescinds the infraction provided. This doesn't delete the infraction, but hides it.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("id")
                    .WithDescription("The ID of the infraction.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("reason")
                    .WithDescription("The reason for rescinding this infraction.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(rescindInfractionCmd);
        var unRescindcmd = new LocalSlashCommand()
            .WithName("infraction-unrescind")
            .WithDescription("Un-rescinds the infraction provided.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("id")
                    .WithDescription("The ID of the infraction.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("reason")
                    .WithDescription("The reason for un-rescinding this infraction.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(unRescindcmd);
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
        var unmuteCmd = new LocalSlashCommand()
            .WithName("unmute")
            .WithDescription("Unmutes the user provided.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("user")
                    .WithDescription("The user to unmute.")
                    .WithType(SlashCommandOptionType.User)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("reason")
                    .WithDescription("The reason for unmuting the user.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(unmuteCmd);
        var unbanCmd = new LocalSlashCommand()
            .WithName("unban")
            .WithDescription("Unbans the user provided.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("user")
                    .WithDescription("The user to unban(ID only)")
                    .WithType(SlashCommandOptionType.User)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("reason")
                    .WithDescription("The reason for the unban.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(unbanCmd);
        var kickCmd = new LocalSlashCommand()
            .WithName("kick")
            .WithDescription("Kicks the user provided.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("user")
                    .WithDescription("The user to kick.")
                    .WithType(SlashCommandOptionType.User)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("reason")
                    .WithDescription("The reason for the kick.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(kickCmd);
        var roleAddCmd = new LocalSlashCommand()
            .WithName("role-add")
            .WithDescription("Adds a role to yourself.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("role")
                    .WithDescription("The role to add.")
                    .WithType(SlashCommandOptionType.Role)
                    .WithIsRequired()
            });
        cmds.Add(roleAddCmd);
        var roleRemoveCmd = new LocalSlashCommand()
            .WithName("role-remove")
            .WithDescription("Removes a role from yourself.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("role")
                    .WithDescription("The role to remove.")
                    .WithType(SlashCommandOptionType.Role)
                    .WithIsRequired()
            });
        cmds.Add(roleRemoveCmd);
        var configRoleAddCmd = new LocalSlashCommand()
            .WithName("configure-role-add")
            .WithDescription("Adds a role to the list of self assignabale roles for your guild.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("role")
                    .WithDescription("The role to add.")
                    .WithType(SlashCommandOptionType.Role)
                    .WithIsRequired()
            });
        cmds.Add(configRoleAddCmd);
        var configRoleRemoveCmd = new LocalSlashCommand()
            .WithName("configure-role-remove")
            .WithDescription("Removes a role from the list self assignable roles for your guild.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("role")
                    .WithDescription("The role to remove.")
                    .WithType(SlashCommandOptionType.Role)
                    .WithIsRequired()
            });
        cmds.Add(configRoleRemoveCmd);
        var roleHelpCmd = new LocalSlashCommand()
            .WithName("role")
            .WithDescription("Shows a helpful embed describing how to add and remove roles from yourself.");
        cmds.Add(roleHelpCmd);
        var configReqRoleCmd = new LocalSlashCommand()
            .WithName("configure-requiredrole")
            .WithDescription("Sets the required role that members must have for Unix to listen to commands for.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("id")
                    .WithDescription("The ID of the role(provide the guild ID if you don't want a role gate)")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(configReqRoleCmd);
        var remindCmd = new LocalSlashCommand()
            .WithName("remind")
            .WithDescription("Sets yourself a reminder.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("duration")
                    .WithDescription("The duration of the reminder(when do you want to be reminded).")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("message")
                    .WithDescription("What do you want to be reminded of?")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(remindCmd);
        var deleteRemindCmd = new LocalSlashCommand()
            .WithName("reminder-delete")
            .WithDescription("Deletes one of your reminders.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("id")
                    .WithDescription("The ID of the reminder you want to delete.")
                    .WithType(SlashCommandOptionType.Integer)
                    .WithIsRequired()
            });
        cmds.Add(deleteRemindCmd);
        var reminderCmd = new LocalSlashCommand()
            .WithName("reminders")
            .WithDescription("Fetches a list of your current reminders.");
        cmds.Add(reminderCmd);
        var configPhishermanCmd = new LocalSlashCommand()
            .WithName("configure-phisherman")
            .WithDescription("Configures your guild's API key to the phisherman API.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("key")
                    .WithDescription("Your API key.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(configPhishermanCmd);
        var tagCmd = new LocalSlashCommand()
            .WithName("tag")
            .WithDescription("Uses a tag, sending back it's response.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("name")
                    .WithDescription("The name of the tag.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        var createTagCmd = new LocalSlashCommand()
            .WithName("tag-create")
            .WithDescription("Creates a tag.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("name")
                    .WithDescription("The name of the new tag.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("content")
                    .WithDescription("The content that the tag should hold.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(createTagCmd);
        cmds.Add(tagCmd);
        var tagsCmd = new LocalSlashCommand()
            .WithName("tags")
            .WithDescription("Fetches a list of tags.");
        cmds.Add(tagsCmd);
        var editTagCmd = new LocalSlashCommand()
            .WithName("tag-edit")
            .WithDescription("Edits a tags content.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("name")
                    .WithDescription("The name of the tag.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("content")
                    .WithDescription("The new content of the tag.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(editTagCmd);
        var transferTagCmd = new LocalSlashCommand()
            .WithName("tag-transfer")
            .WithDescription("Transfers ownership of a tag to another user.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("name")
                    .WithDescription("The name of the tag.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("user")
                    .WithDescription("The user to transfer the tag to.")
                    .WithType(SlashCommandOptionType.User)
                    .WithIsRequired()
            });
        cmds.Add(transferTagCmd);
        var deleteTagCmd = new LocalSlashCommand()
            .WithName("tag-delete")
            .WithDescription("Deletes a tag.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("name")
                    .WithDescription("The name of the tag.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(deleteTagCmd);
        var botInfoCmd = new LocalSlashCommand()
            .WithName("botinfo")
            .WithDescription("Displays information about Unix.");
        cmds.Add(botInfoCmd);
        var infoCmd = new LocalSlashCommand()
            .WithName("info")
            .WithDescription("Gets information about a user.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("user")
                    .WithDescription("The user to get information about.")
                    .WithType(SlashCommandOptionType.User)
            });
        cmds.Add(infoCmd);
        var reactionRoleAddCmd = new LocalSlashCommand()
            .WithName("reaction-role-create")
            .WithDescription("Creates a reaction role.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("role")
                    .WithDescription("The role.")
                    .WithType(SlashCommandOptionType.Role)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("channel")
                    .WithDescription("The channel that the message resides in.")
                    .WithType(SlashCommandOptionType.Channel)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("message")
                    .WithDescription("The ID of the message.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired(),
                new LocalSlashCommandOption()
                    .WithName("emoji")
                    .WithDescription("The ID of the emoji.")
                    .WithType(SlashCommandOptionType.String)
                    .WithIsRequired()
            });
        cmds.Add(reactionRoleAddCmd);
        var fetchReactionRolesCmd = new LocalSlashCommand()
            .WithName("reaction-roles")
            .WithDescription("Fetches all reaction roles in your guild.");
        cmds.Add(fetchReactionRolesCmd);
        var reactionRoleDeleteCmd = new LocalSlashCommand()
            .WithName("reaction-role-delete")
            .WithDescription("Removes a reaction role.")
            .WithOptions(new[]
            {
                new LocalSlashCommandOption()
                    .WithName("id")
                    .WithDescription("The ID of the reaction role(use /reaction-roles to view reaction roles with their ID's).")
                    .WithType(SlashCommandOptionType.Integer)
                    .WithIsRequired()
            });
        cmds.Add(reactionRoleDeleteCmd);
        await Bot.SetGlobalApplicationCommandsAsync(Bot.CurrentUser.Id, cmds);
    }
}