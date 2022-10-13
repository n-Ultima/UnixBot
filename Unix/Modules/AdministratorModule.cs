using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Qmmands;
using Unix.Data.Migrations;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules;

[SlashGroup("configure")]
public class AdministratorModule : UnixAdministratorModuleBase
{
    public AdministratorModule(IGuildService guildConfigurationService) : base(guildConfigurationService)
    {
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
                    break;
                case false:
                    return Success("AutoMod is successfully disabled.");
                    break;
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
}