using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Qmmands;
using Unix.Modules.Bases;
using Unix.Modules.Checks;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules;

public class ReactionRoleModule : UnixModuleBase
{
    private readonly IReactionRoleService _reactionRoleService;
    public ReactionRoleModule(IGuildService guildConfigurationService, IReactionRoleService reactionRoleService) : base(guildConfigurationService)
    {
        _reactionRoleService = reactionRoleService;
    }
    [SlashCommand("reaction-roles")]
    [RequireGuildAdministrator]
    [Description("Lists reaction roles.")]
    public async Task<IResult> ListReactionRoleAsync()
    {
        var guild = Context.Author.GetGuild();
        var reactionRoleEmbed = new LocalEmbed()
            .WithTitle("Reaction Roles")
            .WithColor(Color.Purple)
            .WithAuthor(guild.Name, guild.GetIconUrl() ?? null);
        var reactionRoles = await _reactionRoleService.FetchReactionRolesAsync(guild.Id);
        if (!reactionRoles.Any())
        {
            return EphmeralFailure("No reaction roles exist for your guild.");
        }

        foreach (var role in reactionRoles)
        {
            var guildRoles = guild.Roles;
            var roleName = guildRoles.Where(x => x.Value.Id == role.RoleId).FirstOrDefault().Value;
            reactionRoleEmbed.AddField($"({role.Id})Message ID: {role.MessageId}", $"Reacting with <:emoji:{role.EmojiId}> -> will grant **{roleName.Name}**");
        }

        return Response(new LocalInteractionMessageResponse()
            .WithIsEphemeral()
            .WithEmbeds(reactionRoleEmbed));
    }
}