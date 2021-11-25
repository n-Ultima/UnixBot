using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Qmmands;
using Unix.Data.Models.Moderation;
using Unix.Modules.Attributes;
using Unix.Modules.Bases;
using Unix.Services.Core;

namespace Unix.Modules
{
    public class ModerationModule : UnixGuildModuleBase
    {
        private readonly ModerationService _moderationService;

        public ModerationModule(ModerationService moderationService)
        {
            _moderationService = moderationService;
        }
        [Command("warn")]
        [Description("Warns a user.")]
        [RequireGuildModerator]
        public async Task<DiscordCommandResult> WarnAsync(
            [Description("The user to warn.")] 
                IMember member,
            [Description("The reason for the warn.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, member);
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.Author.Id, member.Id, InfractionType.Warn, reason, null);
            return Success($"Warned **{member.Tag}** | `{reason}`");
        }

        [Command("note", "notice")]
        [Description("Records a note for the user.")]
        [RequireGuildModerator]
        public async Task<DiscordCommandResult> NoteAsync(
            [Description("The member to record the notice for.")]
                IMember member,
            [Description("The content of the note.")] [Remainder]
                string content)
        {
            RequireHigherRank(Context.Author, member);
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.Author.Id, member.Id, InfractionType.Note, content, null);
            return Success($"Note recorded for **{member.Tag}** | `{content}`");
        }

        [Command("kick")]
        [Description("Kicks the provided member from the guild.")]
        [RequireGuildModerator]
        public async Task<DiscordCommandResult> KickAsync(
            [Description("The member to kick.")] 
                IMember member,
            [Description("The reason for the kick.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, member);
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.Author.Id, member.Id, InfractionType.Kick, reason, null);
            return Success($"Kicked **{member.Tag}** | `{reason}`");
        }

        [Command("ban")]
        [Description("Bans the provided member from the guild.")]
        [RequireGuildModerator]
        public async Task<DiscordCommandResult> BanAsync(
            [Description("The member to ban.")] 
                IMember member,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, member);
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.Author.Id, member.Id, InfractionType.Ban, reason, null);
            return Success($"Banned **{member.Tag}** | `{reason}`");
        }

        [Command("ban")]
        [Description("Bans the provider member temporarily.")]
        [RequireGuildModerator]
        public async Task<DiscordCommandResult> BanAsync(
            [Description("The member to ban.")] 
                IMember member,
            [Description("The duration of the ban.")]
                TimeSpan duration,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, member);
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.Author.Id, member.Id, InfractionType.Ban, reason, duration);
            return Success($"Banned **{member.Tag}** | `{reason}`");
        }

        [Command("ban")]
        [Description("Bans the provided member from the guild.")]
        [RequireGuildModerator]
        public async Task<DiscordCommandResult> BanAsync(
            [Description("The member to ban.")] 
                Snowflake userId,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            var member = Context.Guild.GetMember(userId);
            var user = await Bot.FetchUserAsync(userId);
            if (member != null)
            {
                return await BanAsync(member, reason);
            }
            else
            {
                if (!await PromptAsync(new LocalMessage().WithContent($"The user ID provided({user.Tag}) was not found in the guild. Would you like to force ban them?")))
                {
                    return null;
                } 
            }
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.Author.Id, userId, InfractionType.Ban, reason, null);
            return Success($"Banned **{user.Tag}** | `{reason}`");
        }
        
        [Command("ban")]
        [Description("Bans the provided member from the guild temporarily.")]
        [RequireGuildModerator]
        public async Task<DiscordCommandResult> BanAsync(
            [Description("The member to ban.")] 
                Snowflake userId,
            [Description("The duration of the ban.")]
                TimeSpan duration,
            [Description("The reason for the ban.")] [Remainder]
                string reason)
        {
            var member = Context.Guild.GetMember(userId);
            var user = await Bot.FetchUserAsync(userId);
            if (member != null)
            {
                return await BanAsync(member, duration, reason);
            }
            else
            {
                if (!await PromptAsync(new LocalMessage().WithContent($"The user ID provided({user.Tag}) was not found in the guild. Would you like to force ban them?")))
                {
                    return null;
                } 
            }
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.Author.Id, userId, InfractionType.Ban, reason, duration);
            return Success($"Banned **{user.Tag}** | `{reason}`");
        }

        [Command("mute")]
        [RequireGuildModerator]
        [Description("Mutes the member provided for the duration provided.")]
        public async Task<DiscordCommandResult> MuteAsync(
            [Description("The member to mute.")] 
                IMember member,
            [Description("The duration of the mute.")]
                TimeSpan duration,
            [Description("The reason of the mute.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, member);
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.Author.Id, member.Id, InfractionType.Mute, reason, duration);
            return Success($"Muted **{member.Tag}** | `{reason}`");
        }
        
        [Command("mute")]
        [RequireGuildModerator]
        [Description("Mutes the member provided.")]
        public async Task<DiscordCommandResult> MuteAsync(
            [Description("The member to mute.")] 
                IMember member,
            [Description("The reason of the mute.")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, member);
            await _moderationService.CreateInfractionAsync(Context.GuildId, Context.Author.Id, member.Id, InfractionType.Mute, reason, null);
            return Success($"Muted **{member.Tag}** | `{reason}`");
        }

        [Command("unmute")]
        [RequireGuildModerator]
        public async Task<DiscordCommandResult> UnmuteAsync(
            [Description("The user to unmute")] 
                IMember member,
            [Description("The reason of the unmute")] [Remainder]
                string reason)
        {
            RequireHigherRank(Context.Author, member);
            var infractions = await _moderationService.FetchInfractionsAsync(member.Id);
            var mute = infractions
                .Where(x => x.Type == InfractionType.Mute)
                .Where(x => x.GuildId == Context.GuildId)
                .SingleOrDefault();
            if (mute == null)
            {
                return Failure("The user is not currently muted.");
            }

            await _moderationService.RemoveInfractionAsync(mute.Id, Context.GuildId, Context.Author.Id, reason);
            return Success($"Unmuted **{member.Tag}** | `{reason}`");
        }

        [Command("unban")]
        [RequireGuildModerator]
        [Description("Unbans a user.")]
        public async Task<DiscordCommandResult> UnbanAsync(
            [Description("The user to unban.")] 
                Snowflake userId,
            [Description("The reason for the unban.")] [Remainder]
                string reason)
        {
            var infractions = await _moderationService.FetchInfractionsAsync(userId);
            var ban = infractions
                .Where(x => x.Type == InfractionType.Ban)
                .Where(x => x.GuildId == Context.GuildId)
                .SingleOrDefault();
            var subject = await Bot.FetchUserAsync(userId);
            if (ban == null)
            {
                if (Context.Guild.FetchBanAsync(userId).Result == null)
                {
                    return Failure("The user is not currently banned.");
                }
                else
                {
                    await Context.Guild.DeleteBanAsync(userId, new DefaultRestRequestOptions
                    {
                        Reason = reason
                    });
                    var moderator = await Bot.FetchUserAsync(Context.Author.Id);
                    await _moderationService.LogInfractionDeletionAsync(ban, moderator, subject, reason);
                }
            }
            await _moderationService.RemoveInfractionAsync(ban.Id, Context.GuildId, Context.Author.Id, reason);
            return Success($"Unbanned **{subject.Tag}** | `{reason}`");
        }
        [Command("purge", "clear")]
        [RequireGuildModerator]
        [Description("Purges the amount of messages that the user sent.")]
        public async Task<DiscordCommandResult> PurgeAsync(
            [Description("The user whose messages to clear.")]
                IMember member,
            [Description("The amount of messages to clear(max 100)")]
                int count)
        {
            var messages = (await Context.Channel.FetchMessagesAsync(100))
                .Where(x => x.Author.Id == member.Id)
                .Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14)
                .Select(x => x.Id)
                .Take(count);
            if (!await PromptAsync(new LocalMessage()
                .WithContent($"You are attempting to purge {count} messages sent by **{member.Tag}**?")))
            {
                return null;
            }

            await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
            return Success($"Purged **{count}** messages sent by **{member.Tag}**");
        }
        
        [Command("purge", "clear")]
        [RequireGuildModerator]
        [Description("Purges the amount of messages that the user sent.")]
        public async Task<DiscordCommandResult> PurgeAsync(
            [Description("The amount of messages to clear(max 100)")]
                int count)
        {
            var messages = (await Context.Channel.FetchMessagesAsync(100))
                .Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14)
                .Select(x => x.Id)
                .Take(count);
            if (!await PromptAsync(new LocalMessage()
                .WithContent($"You are attempting to purge {count} messages?")))
            {
                return null;
            }

            await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
            return Success($"Purged **{count}** messages.");
        }
        
        [Command("purge", "clear")]
        [RequireGuildModerator]
        [Description("Purges the amount of messages that the user sent.")]
        public async Task<DiscordCommandResult> PurgeAsync(
            [Description("The user whose messages to clear.")]
                IMember member,
            [Description("The channel to delete messages from.")]
                ITextChannel channel,
            [Description("The amount of messages to clear(max 100)")]
                int count)
        {
            var messages = (await channel.FetchMessagesAsync(100))
                .Where(x => x.Author.Id == member.Id)
                .Where(x => (DateTimeOffset.UtcNow - x.CreatedAt()).TotalDays <= 14)
                .Select(x => x.Id)
                .Take(count);
            if (!await PromptAsync(new LocalMessage()
                .WithContent($"You are attempting to purge {count} messages sent by **{member.Tag}** in {Mention.Channel(channel)}?")))
            {
                return null;
            }

            await channel.DeleteMessagesAsync(messages);
            return Success($"Purged **{messages.Count()}** messages sent by **{member.Tag}** in {Mention.Channel(channel)}");
        }
        /// <summary>
        ///     Throws if the first member provided does not ourank the second member.
        /// </summary>
        /// <param name="member1"></param>
        /// <param name="member2"></param>
        /// <exception cref="Exception">Thrown if <see cref="member1"/> does not have a higher hiearchy than <see cref="member2"/></exception>
        private void RequireHigherRank(IMember member1, IMember member2)
        {
            if (member1.GetHierarchy() <= member2.GetHierarchy())
            {
                throw new Exception("Executing user requires a higher rank.");
            }
        }
    }
}