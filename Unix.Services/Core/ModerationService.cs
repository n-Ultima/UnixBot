using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;
using Unix.Common;
using Unix.Data;
using Unix.Data.Models.Moderation;

namespace Unix.Services.Core
{
    public class ModerationService : UnixService
    {
        private readonly GuildService _guildService;
        public ModerationService(IServiceProvider serviceProvider, GuildService guildService) : base(serviceProvider)
        {
            _guildService = guildService;
        }

        public Dictionary<Snowflake, Snowflake> GuildModLogIds = new();
        public Dictionary<Snowflake, Snowflake> GuildMuteRoleIds = new();
        public async Task CreateInfractionAsync(Snowflake guildId, Snowflake moderatorId, Snowflake subjectId, InfractionType type, string reason, TimeSpan? duration)
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
                unixContext.Infractions.Add(new Infraction
                {
                    Id = Guid.NewGuid(),
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
                });    
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

                        await guild.CreateBanAsync(subjectId, $"{moderator.Tag} - {reason}");
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

                        await guild.KickMemberAsync(subjectId, new DefaultRestRequestOptions
                        {
                            Reason = $"{moderator.Tag} - {reason}"
                        });
                        goto Log;
                    case InfractionType.Mute:
                        try
                        {
                            await member.SendMessageAsync(new LocalMessage()
                                .WithContent($"You have been muted in {guild.Name} for {duration.Value.Humanize(10)}. Reason: {reason}"));
                        }
                        catch (RestApiException)
                        {
                            
                        }

                        var muteRole = guild.Roles.GetValueOrDefault(guildConfig.MuteRoleId);
                        var gMember = guild.GetMember(subjectId);
                        await gMember.GrantRoleAsync(muteRole.Id, new DefaultRestRequestOptions
                        {
                            Reason = $"{moderator.Tag} - {reason}"
                        });
                        var muteRoleCache = GuildMuteRoleIds[guild.Id];
                        if (muteRoleCache == default)
                        {
                            GuildMuteRoleIds.Add(guild.Id, muteRole.Id);
                        }
                        goto Log;
                }
                Log:
                await LogAsync(guild, member, moderator, type, duration.HasValue
                        ? duration.Value.Humanize(10)
                        : null,
                    reason);

            }
        }

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
        public async Task RemoveInfractionAsync(Guid infractionId, Snowflake guildId, Snowflake removerId, string removalMessage)
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
                        await Bot.DeleteBanAsync(guildId, infraction.SubjectId, new DefaultRestRequestOptions
                        {
                            Reason = removalMessage
                        });
                        goto Log;
                    case InfractionType.Mute:
                        await Bot.RevokeRoleAsync(guildId, infraction.SubjectId, guildConfig.MuteRoleId, new DefaultRestRequestOptions
                        {
                            Reason = removalMessage
                        });
                        goto Log;
                }
                Log:
                await LogInfractionDeletionAsync(infraction, remover, subject, removalMessage);
            }
        }

        public async Task<Infraction> FetchInfractionAsync(Guid infractionId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                return await unixContext.Infractions
                    .FindAsync(infractionId);
            }
        }

        public async Task<IEnumerable<Infraction>> FetchInfractionsAsync(Snowflake userId)
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
        #nullable  enable
        public async Task LogAsync(CachedGuild guild, IRestUser subject, CachedMember moderator, InfractionType type, string? humanizedDuration, string reason)
        {
            if (!GuildModLogIds.ContainsKey(guild.Id))
            {
                var guildConfig = await _guildService.FetchGuildConfigurationAsync(guild.Id);
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
                    .WithContent($"**{subject.Tag}**(`{subject.Id}`) received a notice by **{moderator.Tag}**(`{moderator.Id}`). Reason:\n```\n{reason}\n```"));
            }
            if(type == InfractionType.Mute)
            {
                if (humanizedDuration != null)
                {
                    await Bot.SendMessageAsync(modLog, new LocalMessage()
                        .WithContent($"**{subject.Tag}**(`{subject.Id}`) was {format} by **{moderator.Tag}**(`{moderator.Id}`) for {humanizedDuration}. Reason:\n```\n{reason}\n```"));
                }
                else
                {
                    await Bot.SendMessageAsync(modLog, new LocalMessage()
                        .WithContent($"**{subject.Tag}**(`{subject.Id}`) was {format} indefinitely by **{moderator.Tag}**(`{moderator.Id}`). Reason:\n```\n{reason}\n```"));
                }
            }

            if (type == InfractionType.Ban)
            {
                if (humanizedDuration != null)
                {
                    await Bot.SendMessageAsync(modLog, new LocalMessage()
                        .WithContent($"**{subject.Tag}**(`{subject.Id}`) was temp banned by **{moderator.Tag}**(`{moderator.Id}`) for {humanizedDuration}. Reason:\n```\n{reason}\n```"));
                }
                else
                {
                    await Bot.SendMessageAsync(modLog, new LocalMessage()
                        .WithContent($"**{subject.Tag}**(`{subject.Id}`) was {format} by **{moderator.Tag}**(`{moderator.Id}`). Reason:\n```\n{reason}\n```"));
                }
            }

            if (type == InfractionType.Kick)
            {
                await Bot.SendMessageAsync(modLog, new LocalMessage()
                    .WithContent($"**{subject.Tag}**(`{subject.Id}`) was {format} by **{moderator.Tag}**(`{moderator.Id}`). Reason:\n```\n{reason}\n```"));
            }

            if (type == InfractionType.Warn)
            {
                await Bot.SendMessageAsync(modLog, new LocalMessage()
                    .WithContent($"**{subject.Tag}**(`{subject.Id}`) was {format} by **{moderator.Tag}**(`{moderator.Id}`). Reason:\n```\n{reason}\n```"));
            }
            
        }
        
        #nullable  disable
        public async Task LogInfractionDeletionAsync(Infraction infraction, IRestUser infractionRemover, IRestUser infractionSubject, string reason)
        {
            Snowflake modLog = default;
            Snowflake muteRole = default;
            if (!GuildModLogIds.TryGetValue(infraction.GuildId, out _))
            {
                var guildConfig = await _guildService.FetchGuildConfigurationAsync(infraction.GuildId);
                GuildModLogIds.Add(infraction.GuildId, guildConfig.ModLogChannelId);
                modLog = GuildModLogIds[infraction.GuildId];
            }
            if (!GuildMuteRoleIds.TryGetValue(infraction.GuildId, out _))
            {
                var guildConfig = await _guildService.FetchGuildConfigurationAsync(infraction.GuildId);
                GuildMuteRoleIds.Add(infraction.GuildId, guildConfig.MuteRoleId);
                muteRole = GuildMuteRoleIds[infraction.GuildId];
            }
            // Modlog has a value now.
            if (infraction.Type == InfractionType.Ban)
            {
                // Remove the ban obviously.
                await Bot.DeleteBanAsync(GuildModLogIds.FirstOrDefault(x => x.Value == modLog).Key, infraction.SubjectId, new DefaultRestRequestOptions
                {
                    Reason = $"{infractionRemover.Tag} - {reason}"
                });
                // Log
                await Bot.SendMessageAsync(modLog, new LocalMessage()
                    .WithContent($"**{infractionSubject.Tag}**(`{infractionSubject.Id}`) was unbanned by **{infractionRemover.Tag}**(`{infractionRemover.Id}`). Reason:\n```\n{reason}\n```"));
            }

            if (infraction.Type == InfractionType.Kick || infraction.Type == InfractionType.Note || infraction.Type == InfractionType.Warn)
            {
                await Bot.SendMessageAsync(modLog, new LocalMessage()
                    .WithContent($"Infraction `{infraction.Id}` was removed by {infractionRemover.Tag}(`{infractionRemover.Id}`). Reason:\n```\n{reason}\n```"));
            }

            if (infraction.Type == InfractionType.Mute)
            {
                await Bot.RevokeRoleAsync(GuildModLogIds.FirstOrDefault(x => x.Value == modLog).Key, infraction.SubjectId, muteRole, new DefaultRestRequestOptions
                {
                    Reason = $"{infractionRemover.Tag} - {reason}"
                });
            }
            
        }
        
        
    }
}