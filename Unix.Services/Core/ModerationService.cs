using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Unix.Data;
using Unix.Data.Models.Moderation;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.Core;

public class ModerationService : UnixService, IModerationService
{
    private readonly GuildService _guildService;
    public ModerationService(IServiceProvider serviceProvider, GuildService guildService) : base(serviceProvider)
    {
        _guildService = guildService;
    }

    public Dictionary<Snowflake, Snowflake> GuildModLogIds = new();

    /// <inheritdoc /> 
    public async Task CreateInfractionAsync(Snowflake guildId, Snowflake moderatorId, Snowflake subjectId, InfractionType type, string reason, bool manual, TimeSpan? duration)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var guild = Bot.GetGuild(guildId);
            var member = await Bot.FetchUserAsync(subjectId);
            var moderator = guild.GetMember(moderatorId);
            if (member == null)
            {
                throw new Exception("Please provide a valid user ID.");
            }
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var bans = await unixContext.Infractions
                .Where(x => x.Type == InfractionType.Ban)
                .Where(x => x.SubjectId == subjectId)
                .Where(x => x.GuildId == guildId)
                .SingleOrDefaultAsync();
            var mutes = await unixContext.Infractions
                .Where(x => x.Type == InfractionType.Mute)
                .Where(x => x.SubjectId == subjectId)
                .Where(x => x.GuildId == guildId)
                .SingleOrDefaultAsync();
            var now = DateTimeOffset.UtcNow;
            if (mutes != null && type == InfractionType.Mute)
            {
                if (duration.HasValue)
                {
                    mutes.Duration = duration;
                    mutes.ExpiresAt = now + duration;
                }
                else
                {
                    mutes.Duration = null;
                }

                mutes.CreatedAt = now;
                await unixContext.SaveChangesAsync();
                goto Log;

            }
            if (bans != null && type == InfractionType.Ban)
            {
                if (duration.HasValue)
                {
                    bans.Duration = duration;
                    bans.ExpiresAt = now + duration;
                }
                else
                {
                    bans.Duration = null;
                }

                bans.CreatedAt = now;
                await unixContext.SaveChangesAsync();
                goto Log;

            }

            var infId = Guid.NewGuid();
            var newInfraction = new Infraction
            {
                Id = infId,
                GuildId = guildId,
                CreatedAt = now,
                ExpiresAt = duration.HasValue
                    ? now + duration
                    : null,
                Duration = duration.HasValue
                    ? duration
                    : null,
                Reason = reason,
                ModeratorId = moderatorId,
                Type = type,
                SubjectId = subjectId
            };
            unixContext.Infractions.Add(newInfraction);
            await unixContext.SaveChangesAsync();
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(guild.Id);
            switch (type)
            {
                case InfractionType.Warn:
                    try
                    {
                        await member.SendMessageAsync(new LocalMessage()
                            .WithContent($"You have been warned in {guild.Name}. Reason: {reason}"));
                    }
                    catch (RestApiException)
                    {

                    }
                    goto Log;
                case InfractionType.Note:
                    break;
                case InfractionType.Ban:
                    try
                    {
                        await member.SendMessageAsync(new LocalMessage()
                            .WithContent($"You have been banned from {guild.Name}. Reason: {reason}"));
                    }
                    catch (RestApiException)
                    {

                    }

                    if (!manual)
                    {
                        await guild.CreateBanAsync(subjectId, $"{moderator.Tag} - {reason}");
                    }
                    goto Log;
                case InfractionType.Kick:
                    try
                    {
                        await member.SendMessageAsync(new LocalMessage()
                            .WithContent($"You have been kicked from {guild.Name}. Reason: {reason}"));
                    }
                    catch (RestApiException)
                    {

                    }

                    if (!manual)
                    {
                        await guild.KickMemberAsync(subjectId, new DefaultRestRequestOptions
                        {
                            Reason = $"{moderator.Tag} - {reason}"
                        });
                    }
                    goto Log;
                case InfractionType.Mute:
                    try
                    {
                        await member.SendMessageAsync(new LocalMessage()
                            .WithContent($"You have been timed out in {guild.Name} for {duration.Value.Humanize(10)}. Reason: {reason}"));
                    }
                    catch (RestApiException)
                    {

                    }

                    var gMember = guild.GetMember(subjectId);
                    await gMember.ModifyAsync(x => x.TimedOutUntil = now + duration);
                    goto Log;
            }
        Log:
            await LogAsync(guild, member, moderator, type, duration.HasValue
                    ? duration.Value.Humanize(10)
                    : null,
                reason);
        }
    }

    /// <inheritdoc /> 
    public async Task<IEnumerable<Infraction>> FetchTimedInfractionsAsync()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.Infractions
                .Where(x => x.Duration != null)
                .ToListAsync();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Infraction>> FetchInfractionsByModeratorAsync(Snowflake guildId, Snowflake userId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var infractions = await unixContext.Infractions
                .Where(x => x.GuildId == guildId)
                .Where(x => x.ModeratorId == userId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
            return infractions;
        }
    }
    /// <inheritdoc /> 
    public async Task UpdateInfractionAsync(Guid infractionId, Snowflake guildId, string newReason)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var infraction = await unixContext.Infractions
                .Where(x => x.GuildId == guildId)
                .Where(x => x.Id == infractionId)
                .SingleOrDefaultAsync();
            if (infraction == null)
            {
                throw new Exception("The infraction ID provided is not valid.");
            }

            infraction.Reason = newReason;
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc /> 
    public async Task RemoveInfractionAsync(Guid infractionId, Snowflake guildId, Snowflake removerId, bool manual, string removalMessage)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            if (string.IsNullOrWhiteSpace(removalMessage))
            {
                throw new NullReferenceException(nameof(removalMessage));
            }
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var infraction = await unixContext.Infractions
                .Where(x => x.GuildId == guildId)
                .Where(x => x.Id == infractionId)
                .SingleOrDefaultAsync();
            if (infraction == null)
            {
                throw new Exception("The infraction ID provided is not valid.");
            }

            var remover = await Bot.FetchUserAsync(removerId);
            var subject = await Bot.FetchUserAsync(infraction.SubjectId);
            var guildConfig = await unixContext.GuildConfigurations.FindAsync(guildId);
            unixContext.Infractions.Remove(infraction);
            await unixContext.SaveChangesAsync();
            switch (infraction.Type)
            {
                case InfractionType.Ban:
                    if (!manual)
                    {
                        await Bot.DeleteBanAsync(guildId, infraction.SubjectId, new DefaultRestRequestOptions
                        {
                            Reason = removalMessage
                        });
                    }
                    goto Log;
                case InfractionType.Mute:
                    await Bot.ModifyMemberAsync(guildId, infraction.SubjectId, x => x.TimedOutUntil = null, new DefaultRestRequestOptions
                    {
                        Reason = $"{remover.Tag} - {removalMessage}"
                    });
                    goto Log;
            }
        Log:
            await LogInfractionDeletionAsync(infraction, remover, subject, manual, removalMessage);
        }
    }

    /// <inheritdoc />
    public async Task RescindInfractionAsync(Guid infractionId, Snowflake guildId, Snowflake removerId, string rescindMessage)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var infraction = await unixContext.Infractions
                .Where(x => x.GuildId == guildId)
                .Where(x => x.Id == infractionId)
                .SingleOrDefaultAsync();
            if (infraction == null)
            {
                throw new Exception("That infraction does not exist.");
            }

            if (infraction.Type == InfractionType.Mute || infraction.Type == InfractionType.Ban)
            {
                throw new Exception($"You cannot rescind infractions of type {infraction.Type}");
            }

            if (infraction.IsRescinded)
            {
                throw new Exception("The infraction provided is already rescinded.");
            }

            infraction.IsRescinded = true;
            await unixContext.SaveChangesAsync();
            var guild = Bot.GetGuild(guildId);
            var moderator = guild.GetMember(removerId);
            await LogInfractionRescinsionAsync(infraction, moderator, rescindMessage);
        }
    }

    /// <inheritdoc />
    public async Task UnRescindInfractionAsync(Guid infractionId, Snowflake guildId, Snowflake moderatorId, string reason)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var infraction = await unixContext.Infractions
                .Where(x => x.GuildId == guildId)
                .Where(x => x.Id == infractionId)
                .SingleOrDefaultAsync();
            if (infraction == null)
            {
                throw new Exception("That infraction does not exist.");
            }

            if (!infraction.IsRescinded)
            {
                throw new Exception($"This infraction is not currently rescinded.");
            }

            infraction.IsRescinded = false;
            await unixContext.SaveChangesAsync();
            var guild = Bot.GetGuild(guildId);
            var moderator = guild.GetMember(moderatorId);
            await LogInfractionUnRescinsionAsync(infraction, moderator, reason);
        }
    }

    /// <inheritdoc /> 
    public async Task<Infraction> FetchInfractionAsync(Guid infractionId, Snowflake guildId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.Infractions
                .Where(x => x.GuildId == guildId)
                .Where(x => x.Id == infractionId)
                .SingleOrDefaultAsync();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Infraction>> FetchInfractionsAsync(Snowflake guildId, Snowflake userId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.Infractions
                .Where(x => x.SubjectId == userId)
                .AsNoTracking()
                .ToListAsync();
        }
    }
#nullable enable
    /// <inheritdoc /> 
    public async Task LogAsync(CachedGuild guild, IUser subject, IUser moderator, InfractionType type, string? humanizedDuration, string reason)
    {
        if (!GuildModLogIds.ContainsKey(guild.Id))
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(guild.Id);
            if (guildConfig.ModLogChannelId == default)
            {
                return;
            }
            GuildModLogIds.Add(guild.Id, guildConfig.ModLogChannelId);
        }

        var modLog = GuildModLogIds[guild.Id];
        string format = string.Empty;
        switch (type)
        {
            case InfractionType.Ban:
                format = "banned";
                break;
            case InfractionType.Mute:
                format = "muted";
                break;
            case InfractionType.Warn:
                format = "warned";
                break;
            case InfractionType.Kick:
                format = "kicked";
                break;
            case InfractionType.Note:
                break;
            default:
                throw new Exception("Give me a valid format.");
        }

        if (type == InfractionType.Note)
        {
            await Bot.SendMessageAsync(modLog, new LocalMessage()
                .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)}**{subject.Tag}**(`{subject.Id}`) received a notice by **{moderator.Tag}**(`{moderator.Id}`). Reason:```{reason}```"));
        }
        if (type == InfractionType.Mute)
        {
            if (humanizedDuration != null)
            {
                await Bot.SendMessageAsync(modLog, new LocalMessage()
                    .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)}**{subject.Tag}**(`{subject.Id}`) was {format} by **{moderator.Tag}**(`{moderator.Id}`) for {humanizedDuration}. Reason:```{reason}```"));
            }
            else
            {
                await Bot.SendMessageAsync(modLog, new LocalMessage()
                    .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)}**{subject.Tag}**(`{subject.Id}`) was {format} indefinitely by **{moderator.Tag}**(`{moderator.Id}`). Reason:```{reason}```"));
            }
        }

        if (type == InfractionType.Ban)
        {
            if (humanizedDuration != null)
            {
                await Bot.SendMessageAsync(modLog, new LocalMessage()
                    .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)}**{subject.Tag}**(`{subject.Id}`) was temp banned by **{moderator.Tag}**(`{moderator.Id}`) for {humanizedDuration}. Reason:```{reason}```"));
            }
            else
            {
                await Bot.SendMessageAsync(modLog, new LocalMessage()
                    .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)}**{subject.Tag}**(`{subject.Id}`) was {format} by **{moderator.Tag}**(`{moderator.Id}`). Reason:```{reason}```"));
            }
        }

        if (type == InfractionType.Kick)
        {
            await Bot.SendMessageAsync(modLog, new LocalMessage()
                .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)}**{subject.Tag}**(`{subject.Id}`) was {format} by **{moderator.Tag}**(`{moderator.Id}`). Reason:```{reason}```"));
        }

        if (type == InfractionType.Warn)
        {
            await Bot.SendMessageAsync(modLog, new LocalMessage()
                .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)}**{subject.Tag}**(`{subject.Id}`) was {format} by **{moderator.Tag}**(`{moderator.Id}`). Reason:```{reason}```"));
        }

    }

#nullable disable
    /// <inheritdoc /> 
    public async Task LogInfractionDeletionAsync(Infraction infraction, IUser infractionRemover, IUser infractionSubject, bool manual, string reason)
    {
        Snowflake modLog = default;
        if (!GuildModLogIds.TryGetValue(infraction.GuildId, out _))
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(infraction.GuildId);
            if (guildConfig.ModLogChannelId == default)
            {
                return;
            }
            GuildModLogIds.Add(infraction.GuildId, guildConfig.ModLogChannelId);
            modLog = GuildModLogIds[infraction.GuildId];
        }

        modLog = GuildModLogIds[infraction.GuildId];
        // Modlog has a value now.
        if (infraction.Type == InfractionType.Ban)
        {
            if (!manual)
            {
                // Remove the ban obviously.
                await Bot.DeleteBanAsync(GuildModLogIds.FirstOrDefault(x => x.Value == modLog).Key, infraction.SubjectId, new DefaultRestRequestOptions
                {
                    Reason = $"{infractionRemover.Tag} - {reason}"
                });
            }
            // Log
            await Bot.SendMessageAsync(modLog, new LocalMessage()
                .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)} **{infractionSubject.Tag}**(`{infractionSubject.Id}`) was unbanned by **{infractionRemover.Tag}**(`{infractionRemover.Id}`). Reason:```{reason}```"));
        }

        if (infraction.Type == InfractionType.Kick || infraction.Type == InfractionType.Note || infraction.Type == InfractionType.Warn)
        {
            await Bot.SendMessageAsync(modLog, new LocalMessage()
                .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)} Infraction `{infraction.Id}` was removed by **{infractionRemover.Tag}**(`{infractionRemover.Id}`). Reason:```{reason}```"));
        }

        if (infraction.Type == InfractionType.Mute)
        {
            if (!manual)
            {
                await Bot.ModifyMemberAsync(infraction.GuildId, infraction.SubjectId, x => x.TimedOutUntil = null, new DefaultRestRequestOptions
                {
                    Reason = $"{infractionRemover.Tag} - {reason}"
                });
            }
            await Bot.SendMessageAsync(modLog, new LocalMessage()
                .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)}**{infractionSubject.Tag}**(`{infractionSubject.Id}`) had their timeout removed by **{infractionRemover.Tag}**(`{infractionRemover.Id}`). Reason:```{reason}```"));
        }

    }

    /// <inheritdoc />
    public async Task LogInfractionRescinsionAsync(Infraction infraction, IUser moderator, string reason)
    {
        Snowflake modLog = default;
        if (!GuildModLogIds.TryGetValue(infraction.GuildId, out _))
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(infraction.GuildId);
            if (guildConfig.ModLogChannelId == default)
            {
                return;
            }
            GuildModLogIds.Add(infraction.GuildId, guildConfig.ModLogChannelId);
            modLog = GuildModLogIds[infraction.GuildId];
        }
        modLog = GuildModLogIds[infraction.GuildId];

        await Bot.SendMessageAsync(modLog, new LocalMessage()
            .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)}Infraction `{infraction.Id}` was rescinded by **{moderator.Tag}**(`{moderator.Id}`). Reason:```{reason}```"));
    }

    /// <inheritdoc />
    public async Task LogInfractionUnRescinsionAsync(Infraction infraction, IUser moderator, string reason)
    {
        Snowflake modLog = default;
        if (!GuildModLogIds.TryGetValue(infraction.GuildId, out _))
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(infraction.GuildId);
            if (guildConfig.ModLogChannelId == default)
            {
                return;
            }
            GuildModLogIds.Add(infraction.GuildId, guildConfig.ModLogChannelId);
            modLog = GuildModLogIds[infraction.GuildId];
        }
        modLog = GuildModLogIds[infraction.GuildId];

        await Bot.SendMessageAsync(modLog, new LocalMessage()
            .WithContent($"{Markdown.Timestamp(DateTimeOffset.UtcNow)}Infraction `{infraction.Id}` was un-rescinded by **{moderator.Tag}**(`{moderator.Id}`). Reason:```{reason}```"));
    }
}