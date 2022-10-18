using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;
using Unix.Data.Models.Moderation;
using Unix.Modules.Bases;
using Unix.Modules.Checks;
using Unix.Services.Core.Abstractions;
using Unix.Services.Extensions;
using Color = System.Drawing.Color;

namespace Unix.Modules;

[SlashGroup("infraction")]
public class InfractionModule : UnixModeratorModuleBase
{
    private readonly IModerationService _moderationService;
    public static Snowflake _autoCompleteGuildId { get; set; }
    public static IMember _autoCompleteMember { get; set; }
    private Dictionary<Snowflake, Infraction> _infractionCache { get; set; } = new();
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

        _infractionCache.Add(Context.GuildId, infraction);
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

        var infraction = await _moderationService.FetchInfractionAsync(infractionId, Context.GuildId);
        if (infraction is null)
        {
            return EphmeralFailure("The ID provided is not a valid infraction ID.");
        }
        
        try
        {
            await _moderationService.UpdateInfractionAsync(infractionId, Context.GuildId, reason);
            _infractionCache.Add(Context.GuildId, infraction);
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
        var infraction = await _moderationService.FetchInfractionAsync(infractionId, Context.GuildId);
        if (infraction is null)
        {
            return EphmeralFailure("The ID provided is not a valid infraction ID.");
        }

        try
        {
            await _moderationService.RescindInfractionAsync(infractionId, Context.GuildId, Context.AuthorId, reason);
            _infractionCache.Add(Context.GuildId, infraction);
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
        var infraction = await _moderationService.FetchInfractionAsync(infractionId, Context.GuildId);
        if (infraction is null)
        {
            return EphmeralFailure("The ID provided is not a valid infraction ID.");
        }
        try
        {
            await _moderationService.UnRescindInfractionAsync(infractionId, Context.GuildId, Context.AuthorId, reason);
            _infractionCache.Add(Context.GuildId, infraction);
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
        var infraction = await _moderationService.FetchInfractionAsync(infractionId, Context.GuildId);
        if (infraction is null)
        {
            return EphmeralFailure("The ID provided is not a valid infraction ID.");
        }
        try
        {
            await _moderationService.RemoveInfractionAsync(infractionId, Context.GuildId, Context.AuthorId, false, reason);
            _infractionCache.Add(Context.GuildId, infraction);
            return Success($"Infraction `{id}` successfully deleted.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [AutoComplete("lookup")]
    [AutoComplete("update")]
    [AutoComplete("rescind")]
    [AutoComplete("reinstate")]
    [AutoComplete("delete")]
    public void HandleInfractionAutoComplete(AutoComplete<string> id)
    {
        if (id.IsFocused)
        {
            if (!_autoCompleteMember.IsModerator())
            {
                return;
            }

            var infractions = _infractionCache
                .Where(x => x.Key == _autoCompleteGuildId)
                .Select(x => x.Value)
                .ToList();
            if (id.RawArgument != null)
            {
                // These are the infractions that are relevant to this guild.
                var sortedInfractions = infractions.OrderBy(x => x.CreatedAt);
                foreach (var infraction in sortedInfractions)
                {
                    if (id.RawArgument.StartsWith(infraction.Id.ToString()[0].ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        id.Choices.Add(infraction.Id.ToString());
                    }
                }                
            }
            else
            {
                id.Choices.AddRange(infractions.Select(x => x.Id.ToString()));
            }

        }
    }
}

// We cant nest things because Diqord hates me.
public class InfractionContextMenuCommandModule : UnixModeratorModuleBase
{
    private readonly IModerationService _moderationService;
    public InfractionContextMenuCommandModule(IGuildService guildConfigurationService, IModerationService moderationService) : base(guildConfigurationService)
    {
        _moderationService = moderationService;
    }
    [UserCommand("Infractions")]
    [Description("Lookup this users infractions.") ]
    public async Task<IResult> LookupAsync(IMember subject)
    {
        var infractions = await _moderationService.FetchInfractionsAsync(Context.GuildId, subject.Id);
        if(!infractions.Any())
        {
            return Response(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent("This user has no infractions."));
        }
        var infractionEmbed = new LocalEmbed()
            .WithTitle($"Infractions for {subject.Tag}")
            .WithColor(Color.Gold);
        foreach (var infraction in infractions)
        {
            var moderator = await SafeFetchUserAsync(infraction.ModeratorId);
            if (infraction.IsRescinded)
            {
                infractionEmbed.AddField($"(RESCINDED) {infraction.Type.ToString().ToUpper()} - ({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {infraction.Reason}");
            }
            else
            {
                infractionEmbed.AddField($"{infraction.Type.ToString().ToUpper()} - ({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {infraction.Reason}");
            }
        }

        return Response(new LocalInteractionMessageResponse()
            .WithIsEphemeral()
            .WithIsEphemeral());
    }
}
