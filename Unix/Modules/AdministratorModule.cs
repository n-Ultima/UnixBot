using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Qmmands;
using Unix.Data.Migrations;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules;

[SlashGroup("configure")]
public class AdministratorModule : UnixAdministratorModuleBase
{
    private readonly IReactionRoleService _reactionRoleService;
    public AdministratorModule(IGuildService guildConfigurationService, IReactionRoleService reactionRoleService) : base(guildConfigurationService)
    {
        _reactionRoleService = reactionRoleService;
    }
    [SlashCommand("auto-mod")]
    [Description("Sets whether automod is enabled or disabled")]
    public async Task<IResult> SetAutoModAsync(bool isEnabled)
    {
        try
        {
            await _guildConfigurationService.ModifyGuildAutomodAsync(Context.GuildId, isEnabled);
            switch (isEnabled)
            {
                case true:
                    return Success("AutoMod is successfully enabled.");
                case false:
                    return Success("AutoMod is successfully disabled.");
            }
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("mod-log")]
    [Description("Sets where moderation actions are logged.")]
    public async Task<IResult> SetModLogAsync(ITextChannel channel)
    {
        try
        {
            await _guildConfigurationService.ModifyGuildModLogChannelIdAsync(Context.GuildId, channel.Id);
            return Success($"Set <#{channel.Id}>(#{channel.Name}, `{channel.Id}`) to receive moderation logs.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    // This configures the message log, where all message edits, and deletions, are logged to the respective channel.
    [SlashCommand("message-log")]
    [Description("Sets where message edits and deletions are logged.")]
    public async Task<IResult> SetMessageLogAsync(ITextChannel channel)
    {
        try
        {
            await _guildConfigurationService.ModifyGuildMessageLogChannelIdAsync(Context.GuildId, channel.Id);
            return Success($"Set <#{channel.Id}>(#{channel.Name}, `{channel.Id}`) to receive message logs.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("phisherman")]
    [Description("Sets the API key for your server.")]
    public async Task<IResult> SetPhishermanKeyAsync(string apiKey)
    {
        try
        {
            await _guildConfigurationService.ModifyGuildPhishermanApiKeyAsync(Context.GuildId, apiKey);
            return Success($"Successfully modified.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("required-role")]
    [Description("Sets the role users must have to use commands. Set to the @everyone role for everyone to.")]
    public async Task<IResult> SetRequiredRoleAsync(IRole role)
    {
        try
        {
            await _guildConfigurationService.ModifyGuildRequiredRoleAsync(Context.GuildId, role.Id);
            if (role.Id == CurrentGuildConfiguration.Id)
            {
                return Success($"Unix will no longer ignore commands from users lacking a role.");
            }
            return Success($"Unix will not ignore commands from users who lack the **{role.Name}** role.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("miscellaneous-log")]
    [Description("Sets the channel where events such as user joins will be logged.")]
    public async Task<IResult> SetMiscLogAsync(ITextChannel channel)
    {
        try
        {
            await _guildConfigurationService.ModifyGuildMiscellaneousLogChannelIdAsync(Context.GuildId, channel.Id);
            return Success($"Set <#{channel.Id}>(#{channel.Name}, `{channel.Id}`) to receive miscellaneous logs.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("add-banned-term")]
    [Description("Adds a term to the list of banned terms")]
    public async Task<IResult> AddBannedTermAsync(string term)
    {
        if (CurrentGuildConfiguration.BannedTerms.Count != 0 && CurrentGuildConfiguration.BannedTerms.Contains(term))
        {
            return EphmeralFailure("That term is already banned.");
        }

        try
        {
            await _guildConfigurationService.AddBannedTermAsync(Context.GuildId, term);
            return Success("Users who send messages with that term will now be warned automatically.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("remove-banned-term")]
    [Description("Removes a term from the list of banned terms.")]
    public async Task<IResult> RemoveBannedTermAsync(string term)
    {
        if (CurrentGuildConfiguration.BannedTerms.Count != 0 && !CurrentGuildConfiguration.BannedTerms.Contains(term))
        {
            return EphmeralFailure("That term is not currently banned.");
        }

        try
        {
            await _guildConfigurationService.RemoveBannedTermAsync(Context.GuildId, term);
            return Success("User who send messages with that term will no longer be warned automatically.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("add-whitelisted-guild")]
    [Description("Adds a guild to the list of invites that won't be deleted.")]
    public async Task<IResult> AddWhitelistedGuildAsync(string guildId)
    {
        if (!Snowflake.TryParse(guildId, out var snowflake))
        {
            return EphmeralFailure("That ID is not a valid ID.");
        }

        var guild = await Bot.FetchGuildAsync(snowflake);
        if (guild is null)
        {
            return EphmeralFailure("That guild does not exist.");
        }

        try
        {
            await _guildConfigurationService.AddWhitelistedInviteAsync(Context.GuildId, guild.Id);
            return Success($"Invites pointing towards **{guild.Name}** will no longer be deleted.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("remove-whitelisted-guild")]
    [Description("Removes a guild from the list of invites that won't be deleted.")]
    public async Task<IResult> DeleteWhitelistedGuildAsync(string guildId)
    {
        if (!Snowflake.TryParse(guildId, out var snowflake))
        {
            return EphmeralFailure("That ID is not a valid ID.");
        }

        var guild = await Bot.FetchGuildAsync(snowflake);
        if (guild is null)
        {
            return EphmeralFailure("That guild does not exist.");
        }

        try
        {
            await _guildConfigurationService.RemoveWhitelistedInviteAsync(Context.GuildId, guild.Id);
            return Success($"Invites pointing towards **{guild.Name}** will now be deleted.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
        
    }
    [SlashCommand("role-add")]
    [Description("Creates a self-assignable role that users can add to themselves. The role must already exist.")]
    public async Task<IResult> CreateRoleAsync(IRole role)
    {
        var botMember = Context.Author.GetGuild().GetMember(Bot.CurrentUser.Id);
        if (role.Position >= botMember.CalculateRoleHierarchy())
        {
            return EphmeralFailure("The role must be lower than Unix's role.");
        }
        try
        {
            await _guildConfigurationService.AddSelfAssignableRoleAsync(Context.GuildId, role.Id);
            return Success($"Users can now give themselves the **{role.Name}** role.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("role-remove")]
    [Description("Removes a self assignable role.")]
    public async Task<IResult> RemoveRoleAsync(IRole role)
    {
        try
        {
            await _guildConfigurationService.RemoveSelfAssignableRoleAsync(Context.GuildId, role.Id);
            return Success($"Users can no longer give themselves the **{role.Name}** role.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }
    
    [SlashCommand("add-auto-role")]
    [Description("Adds an autorole configuration.")]
    public async Task<IResult> CreateAutoRoleAsync(IRole role)
    {
        if (CurrentGuildConfiguration.AutoRoles.Count != 0 && CurrentGuildConfiguration.AutoRoles.Contains(role.Id.RawValue))
        {
            return EphmeralFailure("That role is already an auto role.");
        }

        try
        {
            await _guildConfigurationService.AddAutoRoleAsync(Context.GuildId, role.Id);
            return Success($"Upon joining, users will now be granted the **{role.Name}** role.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("remove-auto-role")]
    [Description("Removes an autorole configuration.")]
    public async Task<IResult> DeleteAutoRoleAsync(IRole role)
    {
        if (CurrentGuildConfiguration.AutoRoles.Count != 0 && !CurrentGuildConfiguration.AutoRoles.Contains(role.Id.RawValue))
        {
            return EphmeralFailure("That role is not currently an auto role.");
        }

        try
        {
            await _guildConfigurationService.RemoveAutoRoleAsync(Context.GuildId, role.Id);
            return Success($"Upon joining, users will no longer be granted the **{role.Name}** role.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }
    
    [SlashCommand("reaction-role-create")]
    [Description("Creates a reaction role.")]
    public async Task<IResult> CreateReactionRoleAsync(IRole role, ITextChannel channel, string messageId, string emojiId)
    {
        if (!Snowflake.TryParse(messageId, out var snowflakeMessageId))
        {
            return EphmeralFailure("The message ID provided is not a valid ID");
        }

        if (!Snowflake.TryParse(emojiId, out var snowflakeEmojiId))
        {
            return EphmeralFailure("The emoji ID provided is not a valid ID.");
        }

        var message = await channel.FetchMessageAsync(snowflakeMessageId);
        if (message is null)
        {
            return EphmeralFailure("The message ID provided does not exist in the channel provided.");
        }

        var emoji = await Context.Author.GetGuild().FetchEmojiAsync(snowflakeEmojiId);
        if (emoji is null)
        {
            return EphmeralFailure("The emoji ID provided does not exist.");
        }
        
        await message.AddReactionAsync(LocalEmoji.FromEmoji(emoji));
        try
        {
            await _reactionRoleService.CreateReactionRoleAsync(Context.GuildId, snowflakeMessageId, snowflakeEmojiId, role.Id);
            var rr = await _reactionRoleService.FetchReactionRoleAsync(Context.GuildId, snowflakeMessageId, snowflakeEmojiId);
            await message.AddReactionAsync(LocalEmoji.FromEmoji(emoji));
            return Success($"Reaction role created, ID `{rr.Id}`");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("reaction-role-delete")]
    [Description("Deletes a reaction role.")]
    public async Task<IResult> DeleteReactionRoleAsync(long id)
    {
        var reactionRole = await _reactionRoleService.FetchReactionRoleAsync(Context.GuildId, id);
        if (reactionRole is null)
        {
            return EphmeralFailure("No reaction role with that ID exists.");
        }

        try
        {
            await _reactionRoleService.DeleteReactionRoleAsync(reactionRole.GuildId, reactionRole.MessageId, reactionRole.EmojiId, reactionRole.RoleId);
            return Success("Reaction role deleted.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }
}