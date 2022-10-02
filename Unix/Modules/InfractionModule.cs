using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;
using Unix.Modules.Bases;
using Unix.Modules.Checks;
using Unix.Services.Core.Abstractions;
using Color = System.Drawing.Color;

namespace Unix.Modules;

[SlashGroup("infraction")]
public class InfractionModule : UnixModeratorModuleBase
{
    private readonly IModerationService _moderationService;
    public InfractionModule(IGuildService guildConfigurationService, IModerationService moderationService) : base(guildConfigurationService)
    {
        _moderationService = moderationService;
    }

    [SlashCommand("lookup")]
    [Description("Looks up an infraction by it's ID.")]
    public async Task<IResult> LookupInfractionAsync(string id)
    {
        if (!Guid.TryParse(id, out var infractionId))
        {
            return EphmeralFailure("The ID provided is not a valid infraction ID.");
        }

        var infraction = await _moderationService.FetchInfractionAsync(infractionId, Context.GuildId);
        if (infraction is null)
        {
            return EphmeralFailure("The ID provided does not lead to an infraction.");
        }

        var subject = await SafeFetchUserAsync(infraction.SubjectId);
        var moderator = await SafeFetchUserAsync(infraction.ModeratorId);
        var infractionEmbed = new LocalEmbed()
            .WithTitle($"Infraction {infraction.Id}")
            .WithColor(Color.Gold);
        if (infraction.IsRescinded)
        {
            infractionEmbed.AddField($"(RESCINDED) {infraction.Type.ToString().ToUpper()} - ({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {infraction.Reason}");
        }
        else
        {
            infractionEmbed.AddField($"{infraction.Type.ToString().ToUpper()} - ({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {infraction.Reason}");
        }

        return Response(infractionEmbed);
    }

    [SlashCommand("list")]
    [Description("Lists the infractions for the user ID provided.")]
    public async Task<IResult> ListInfractionsAsync(string userId, bool showHidden)
    {
        if (!Snowflake.TryParse(userId, out var subjectId))
        {
            return EphmeralFailure("The ID provided is not a valid user ID.");
        }

        var infractions = await _moderationService.FetchInfractionsAsync(Context.GuildId, subjectId);
        var subject = await SafeFetchUserAsync(subjectId);
        var infractionEmbed = new LocalEmbed()
            .WithTitle($"Infractions for {subject.Tag}")
            .WithColor(Color.Gold);
        foreach (var infraction in infractions)
        {
            var moderator = await SafeFetchUserAsync(infraction.ModeratorId);
            if (infraction.IsRescinded)
            {
                if (!showHidden)
                    continue;
                infractionEmbed.AddField($"(RESCINDED) {infraction.Type.ToString().ToUpper()} - ({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {infraction.Reason}");
            }
            else
            {
                infractionEmbed.AddField($"{infraction.Type.ToString().ToUpper()} - ({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {infraction.Reason}");
            }
        }

        return Response(infractionEmbed);
    }

    [SlashCommand("update")]
    [Description("Updates the infraction provided with a new reason.")]
    public async Task<IResult> UpdateInfractionAsync(string id, string reason)
    {
        if (!Guid.TryParse(id, out var infractionId))
        {
            return EphmeralFailure("The ID provided is not a valid infraction ID.");
        }

        try
        {
            await _moderationService.UpdateInfractionAsync(infractionId, Context.GuildId, reason);
            return Success($"Infraction `{id}` successfully updated.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("rescind")]
    [Description("Rescinds an infraction. This does not permanently delete an infraction.")]
    public async Task<IResult> RescindInfractionAsync(string id, string reason)
    {
        if (!Guid.TryParse(id, out var infractionId))
        {
            return EphmeralFailure("The ID provided is not a valid infraction ID.");
        }

        try
        {
            await _moderationService.RescindInfractionAsync(infractionId, Context.GuildId, Context.AuthorId, reason);
            return Success($"Infraction `{id}` rescinded.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("reinstate")]
    [Description("Reinstates an infraction from being rescinded.")]
    public async Task<IResult> ReInstateAsync(string id, string reason)
    {
        if (!Guid.TryParse(id, out var infractionId))
        {
            return EphmeralFailure("The ID provided is not a valid infraction ID.");
        }

        try
        {
            await _moderationService.UnRescindInfractionAsync(infractionId, Context.GuildId, Context.AuthorId, reason);
            return Success($"Infraction `{id}` has been successfully re-instated.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("delete")]
    [Description("Permanently deletes an infraction.")]
    [RequireGuildAdministrator]
    public async Task<IResult> DeleteInfractionAsync(string id, string reason)
    {
        if (!Guid.TryParse(id, out var infractionId))
        {
            return EphmeralFailure("The ID provided is not a valid infraction ID.");
        }

        try
        {
            await _moderationService.RemoveInfractionAsync(infractionId, Context.GuildId, Context.AuthorId, false, reason);
            return Success($"Infraction `{id}` successfully deleted.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }
}