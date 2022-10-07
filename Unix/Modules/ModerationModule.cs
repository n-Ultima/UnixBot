using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Qmmands;
using Unix.Data.Models.Moderation;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;
using Unix.Services.Parsers;

namespace Unix.Modules;

public class ModerationModule : UnixModeratorModuleBase
{
    private readonly IModerationService _moderationService;
    public ModerationModule(IGuildService guildConfigurationService, IModerationService moderationService) : base(guildConfigurationService)
    {
        _moderationService = moderationService;
    }

    [SlashCommand("warn")]
    [Description("Warns a user.")]
    public async Task<IResult> WarnAsync(IMember member, string reason)
    {
        if (!CanModerateAsync(member, Context.Author))
        {
            return EphmeralFailure($"⚠ You must be higher in hierarchy to execute this action on this user.");
        }
        try
        {
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.AuthorId, member.Id, InfractionType.Warn, reason, false, null);
            return Success($"Warned **{member.Tag}** | `{reason}`");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("note")]
    [Description("Creates a note about a user.")]
    public async Task<IResult> NoteAsync(IMember member, string reason)
    {
        try
        {
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.AuthorId, member.Id, InfractionType.Note, reason, false, null);
            return Success($"Note created for **{member.Tag}** | `{reason}`", true);
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("kick")]
    [Description("Kicks a user from the server.")]
    public async Task<IResult> KickAsync(IMember member, string reason)
    {
        if (!CanModerateAsync(member, Context.Author))
        {
            return EphmeralFailure($"⚠ You must be higher in hierarchy to execute this action on this user.");
        }

        try
        {
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.AuthorId, member.Id, InfractionType.Kick, reason, false, null);
            return Success($"Kicked **{member.Tag}** | `{reason}`");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("ban")]
    [Description("Bans a user from the guild.")]
    public async Task<IResult> BanAsync(IMember member, string reason, string duration = null)
    {
        if (!CanModerateAsync(member, Context.Author))
        {
            return EphmeralFailure($"⚠ You must be higher in hierarchy to execute this action on this user.");
        }

        TimeSpan? timeSpan = null;
        if (TimeSpanParser.TryParseTimeSpan(duration, out var banDuration))
        {
            timeSpan = banDuration;
        }

        try
        {
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.AuthorId, member.Id, InfractionType.Ban, reason, false, timeSpan);
            return Success($"Banned **{member.Tag}** | `{reason}`");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("mute")]
    [Description("Mutes a user.")]
    public async Task<IResult> MuteAsync(IMember member, string reason, string duration)
    {
        if (!CanModerateAsync(member, Context.Author))
        {
            return EphmeralFailure($"⚠ You must be higher in hierarchy to execute this action on this user.");
        }

        TimeSpan? timeSpan = null;
        if (TimeSpanParser.TryParseTimeSpan(duration, out var muteDuration))
        {
            timeSpan = muteDuration;
        }
        else
        {
            return EphmeralFailure("The time span provided is not valid(e.g '3 hours' '5 days').");
        }

        if (timeSpan > TimeSpan.FromDays(28))
        {
            return EphmeralFailure("Mutes cannot last longer than 28 days.");
        }

        try
        {
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.AuthorId, member.Id, InfractionType.Mute, reason, false, muteDuration);
            return Success($"Muted **{member.Tag}** for **{timeSpan.Value.Humanize()}** | `{reason}`");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("purge")]
    [Description("Deletes messages from the executing channel.")]
    public async Task<IResult> PurgeAsync([Maximum(100), Minimum(1)] int count, IMember member = null)
    {
        IEnumerable<Snowflake> messages = new List<Snowflake>();
        if (member is null)
        {
            messages = (await Bot.FetchMessagesAsync(Context.ChannelId))
                .Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14)
                .Select(x => x.Id)
                .Take(count);        
        }
        else
        {
            messages = (await Bot.FetchMessagesAsync(Context.ChannelId))
                .Where(x => x.Author.Id == member.Id)
                .Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14)
                .Select(x => x.Id)
                .Take(count);
        }

        try
        {
            await Bot.DeleteMessagesAsync(Context.ChannelId, messages);
            if (member is not null)
            {
                return Success($"Deleted **{count}** messages sent by **{member.Tag}**");
            }

            return Success($"Deleted **{count}** messages.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("unmute")]
    [Description("Unmutes a user who is currently muted.")]
    public async Task<IResult> UnmuteAsync(IMember member, string reason)
    {
        var currentGuildInfractions = await _moderationService.FetchInfractionsAsync(Context.GuildId, member.Id);
        var muteInfraction = currentGuildInfractions
            .Where(x => x.Type == InfractionType.Mute)
            .Where(x => !x.IsRescinded)
            .SingleOrDefault();
        if (muteInfraction is null)
        {
            // The infraction is null, but it could have been a manual creation.
            // Our event listener should automatically create the infraction, this is just an extra safety check.
            
            // If they aren't timed out, or if their value for TimedOut is in the past, then they aren't muted.
            if (!member.TimedOutUntil.HasValue || member.TimedOutUntil.Value < DateTimeOffset.UtcNow)
            {
                return EphmeralFailure("That user is not currently muted.");
            }
            
            // So now we know that the mute is real, so we need to be sure it's not in the past.
            if (member.TimedOutUntil >= DateTimeOffset.UtcNow)
            {
                // Log it, since the manual mute deletion can't be tracked.
                await _moderationService.LogInfractionDeletionAsync(new Infraction() { GuildId = Context.GuildId, Type = InfractionType.Mute }, Context.Author, member, false, reason);
                return Success($"Unmuted **{member.Tag}** | `{reason}`");
            }
        }
        // If it's not null, just remove like normal.
        try
        {
            await _moderationService.RemoveInfractionAsync(muteInfraction.Id, muteInfraction.GuildId, Context.AuthorId, false, reason);
            return Success($"Unmuted **{member.Tag}** | `{reason}`");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }

    [SlashCommand("unban")]
    [Description("Unbans a user.")]
    public async Task<IResult> UnbanAsync(string id, string reason)
    {
        if (!Snowflake.TryParse(id, out var userId))
        {
            return EphmeralFailure("The ID provided is not a valid user ID.");
        }

        var unbanUser = await SafeFetchUserAsync(userId);
        if (unbanUser is null)
        {
            return EphmeralFailure("The ID provided is not a valid user ID.");
        }
        
        
        var unbanUserInfractions = await _moderationService.FetchInfractionsAsync(Context.GuildId, userId);
        var banInfraction = unbanUserInfractions
            .Where(x => x.Type == InfractionType.Ban)
            .SingleOrDefault();
        if (banInfraction == null)
        {
            var banNoInf = await Context.Author.GetGuild().FetchBanAsync(unbanUser.Id);
            if (banNoInf != null)
            {
                await _moderationService.LogInfractionDeletionAsync(new Infraction { GuildId = Context.GuildId, Type = InfractionType.Ban }, Context.Author, banNoInf.User, false, reason);
                return Success($"Unbanned **{banNoInf.User.Tag}** | `{reason}`");
            }
            else
            {
                return EphmeralFailure("That user is not currently banned.");
            }
        }

        try
        {
            await _moderationService.RemoveInfractionAsync(banInfraction.Id, Context.GuildId, Context.AuthorId, false, reason);
            return Success($"Unbanned **{unbanUser.Tag}** | `{reason}`");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }

    }
    public bool CanModerateAsync(IMember subject, IMember moderator)
    {
        if (moderator.CalculateRoleHierarchy() <= subject.CalculateRoleHierarchy())
        {
            return false;
        }

        return true;
    }
}