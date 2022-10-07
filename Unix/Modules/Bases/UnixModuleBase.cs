using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Qmmands;
using Serilog;
using Unix.Common;
using Unix.Data.Models.Core;
using Unix.Services.Core.Abstractions;
using Unix.Services.Extensions;

namespace Unix.Modules.Bases;

public abstract class UnixModuleBase : DiscordApplicationGuildModuleBase
{
    public readonly IGuildService _guildConfigurationService;
    public GuildConfiguration CurrentGuildConfiguration { get; set; }

    public UnixModuleBase(IGuildService guildConfigurationService)
    {
        _guildConfigurationService = guildConfigurationService;
    }
    public override async ValueTask OnBeforeExecuted()
    {
        if (Context.Interaction is not ISlashCommandInteraction)
        {
            throw new Exception("We shouldn't receive this.");
            //TODO: In the event that I implement modals, or anything besides slash commands, this must be changed.
        }

        var guildConfig = await _guildConfigurationService.FetchGuildConfigurationAsync(Context.GuildId);
        if (guildConfig is null)
        {
            Log.Logger.ForContext<UnixModuleBase>().Error("Interaction was attempted in a guild without a proper guild configuration setup.");
            await Context.SendEphmeralErrorAsync("You must have a proper guild configuration setup. Use /config");
            throw new Exception($"No configuration for guild {Context.GuildId}.");
        }
        
        CurrentGuildConfiguration = guildConfig;
        if (!Context.Author.CanUseCommands())
        {
            EphmeralFailure("Missing permissions");
        }
    }

    public IResult Success(string message, bool isEphmeral = false)
    {
        return Response(new LocalInteractionMessageResponse()
            .WithIsEphemeral(isEphmeral)
            .WithContent($"<:unixok:884524202458222662> {message}"));
    }

    public IResult EphmeralFailure(string errorMessage)
    {
        return Response(new LocalInteractionMessageResponse().WithIsEphemeral().WithContent($"⚠ ️{errorMessage}"));
    }

    public IResult EphmeralFailure(PermissionLevel permissionLevel)
    {
        if (permissionLevel == PermissionLevel.Administrator)
        {
            return Response(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent($"⚠️ User lacks the required `Administrator` permission to use this command."));
        }
        else if (permissionLevel == PermissionLevel.Moderator)
        {
            return Response(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent($"⚠️ User lacks the required `Moderator` permission to use this command."));
        }

        // This should never hit this, but C# wants to cover all, never-gonna-happen cases to we'll return null here.
        return null;
    }

    public async Task<IUser> SafeFetchUserAsync(Snowflake userId)
    {
        var member = Bot.GetGuild(CurrentGuildConfiguration.Id).GetMember(userId);
        if (member is null)
        {
            return await Bot.FetchUserAsync(userId);
        }

        return member;
    }

}