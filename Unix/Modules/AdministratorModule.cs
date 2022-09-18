using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules;

public class AdministratorModule : UnixAdministratorModuleBase
{
    public AdministratorModule(IGuildService guildConfigurationService) : base(guildConfigurationService)
    {
    }
    [SlashCommand("configure-automod")]
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

    [SlashCommand("configure-modlog")]
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
    [SlashCommand("configure-messagelog")]
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

    [SlashCommand("configure-phisherman")]
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

    [SlashCommand("configure-requiredrole")]
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

    [SlashCommand("configure-miscellaneouslog")]
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
}