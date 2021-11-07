using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Qmmands;
using Unix.Data.Models.Moderation;
using Unix.Modules.Attributes;
using Unix.Modules.Bases;
using Unix.Services.Core;

namespace Unix.Modules
{
    [Group("infraction", "infractions", "case", "cases")]
    public class InfractionModule : UnixGuildModuleBase
    {
        private readonly ModerationService _moderationService;

        public InfractionModule(ModerationService moderationService)
        {
            _moderationService = moderationService;
        }
        [Command("")]
        [Description("Fetches the infraction information for the GUID provided.")]
        public async Task<DiscordCommandResult> InfractionsAsync(
            [Description("The ID of the infraction.")]
                Guid infractionId)
        {
            var infraction = await _moderationService.FetchInfractionAsync(infractionId);
            if (infraction == null)
            {
                throw new Exception("The infraction ID provided was not found.");
            }

            var moderator = Context.Guild.GetMember(infraction.ModeratorId);
            var subject = await Bot.FetchUserAsync(infraction.SubjectId);
            var embed = new LocalEmbed()
                .AddField($"{infraction.Type.ToString().ToUpper()} - ({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {infraction.Reason}")
                .WithColor(Color.Gold)
                .WithTitle($"Infractions for {subject.Tag}");
            return Response(embed);
        }

        [Command("")]
        [Description("Fetches the infraction(s) for the user provided.")]
        public async Task<DiscordCommandResult> InfractionsAsync(
            [Description("The member to search infractions for.")]
                IMember member)
        {
            var infractions = await _moderationService.FetchInfractionsAsync(member.Id);
            if (!infractions.Any())
            {
                return Response("The member provided has no current infractions.");
            }

            var embed = new LocalEmbed()
                .WithColor(Color.Gold)
                .WithTitle($"Infractions for {member.Tag}");
            foreach (var infraction in infractions)
            {
                var moderator = Context.Guild.GetMember(infraction.ModeratorId);
                embed.AddField($"{infraction.Type.ToString().ToUpper()}({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {infraction.Reason}");
            }

            return Response(embed);
        }
        [Command("")]
        [Description("Fetches the infraction(s) for the user ID provided.")]
        public async Task<DiscordCommandResult> InfractionsAsync(
            [Description("The userId to search infractions for.")]
                Snowflake userId)
        {
            if (Context.Guild.GetMember(userId) != null)
            {
                return await InfractionsAsync(Context.Guild.GetMember(userId));
            }
            var member = await Bot.FetchUserAsync(userId);
            if (member == null)
            {
                throw new Exception("The user ID provided is not valid.");
            }
            var infractions = await _moderationService.FetchInfractionsAsync(userId);
            if (!infractions.Any())
            {
                return Response("The user provided has no current infractions.");
            }
            
            var embed = new LocalEmbed()
                .WithColor(Color.Gold)
                .WithTitle($"Infractions for {member.Tag}");
            foreach (var infraction in infractions)
            {
                var moderator = Context.Guild.GetMember(infraction.ModeratorId);
                embed.AddField($"{infraction.Type.ToString().ToUpper()}({infraction.Id}) - Created On {infraction.CreatedAt.ToString("M")} by {moderator.Tag}", $"Reason: {infraction.Reason}");
            }

            return Response(embed);
        }

        [Command("delete", "rescind", "remove")]
        [Description("Deletes the infraction with the provided ID.")]
        [RequireGuildModerator]
        public async Task<DiscordCommandResult> DeleteInfractionAsync(
            [Description("The ID of the infraction to delete.")]
                Guid infractionId,
            [Description("The reason for deleting the infraction.")] [Remainder]
                string reason)
        {
            await _moderationService.RemoveInfractionAsync(infractionId, Context.GuildId, reason);
            return Success($"Successfully deleted infraction `{infractionId}`");
        }

        [Command("update", "reason")]
        [Description("Updates the reason for the provided infraction ID.")]
        public async Task<DiscordCommandResult> UpdateInfractionAsync(
            [Description("The ID of the infraction to update.")]
                Guid infractionId,
            [Description("The new reason to be applied to the infraction.")] [Remainder]
                string newReason)
        {
            await _moderationService.UpdateInfractionAsync(infractionId, Context.GuildId, newReason);
            return Success($"Successfully updated infraction: {infractionId}");
        }
    }
}