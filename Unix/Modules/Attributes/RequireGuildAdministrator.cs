using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Unix.Data;

namespace Unix.Modules.Attributes
{
    public class RequireGuildAdministrator : DiscordGuildCheckAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(DiscordGuildCommandContext context)
        {
            var roles = context.Author.RoleIds;
            using (var db = new UnixContext())
            {
                var modRole = await db.GuildConfigurations
                    .Where(x => x.Id == context.GuildId)
                    .Select(x => x.ModeratorRoleId)
                    .SingleOrDefaultAsync();
                if (roles.Contains(modRole) || context.Guild.OwnerId == context.Author.Id)
                {
                    return Success();
                }

                throw new Exception("User lacks the required `Administrator` permission to use this command.");
            }
        }
    }
}