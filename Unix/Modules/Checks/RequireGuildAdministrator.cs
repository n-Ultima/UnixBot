using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Qmmands;
using Unix.Common;
using Unix.Data;

namespace Unix.Modules.Checks;

public class RequireGuildAdministrator : DiscordGuildCheckAttribute
{
    public override async ValueTask<IResult> CheckAsync(IDiscordGuildCommandContext context)
    {
        using (var unixContext = new UnixContext())
        {
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(context.GuildId);
            // Since UnixModuleBase already checks to be sure there is a configuration, no need to null check here.
            if ((context.Author.GetGuild()?.OwnerId != context.AuthorId) || (!context.Author.RoleIds.Contains(guildConfig.AdministratorRoleId.RawValue)) ||
                (context.Author.RoleIds.Count is 0))
            {
                return Results.Failure($"⚠ User lacks the required `Administrator` permission to use this command.");
            }

            return Results.Success;
        }
    }
}
