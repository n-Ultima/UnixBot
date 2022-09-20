using System.Linq;
using System.Threading.Tasks;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Qmmands;
using Unix.Data;

namespace Unix.Modules.Checks;

public class RequireGuildModerator : DiscordGuildCheckAttribute
{
    public override async ValueTask<IResult> CheckAsync(IDiscordGuildCommandContext context)
    {
        using (var unixContext = new UnixContext())
        {
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(context.GuildId);
            if ((context.Author.GetGuild()?.OwnerId != context.AuthorId) || !context.Author.RoleIds.Contains(guildConfig.ModeratorRoleId.RawValue) || !context.Author.RoleIds.Contains(guildConfig.AdministratorRoleId.RawValue) || (context.Author.RoleIds.Count is 0))
            {
                return Results.Failure($"⚠ User lacks the required `Moderator` permission to use this command.");
            }

            return Results.Success;
        }
    }
}