using System.Collections.Concurrent;
using System.Linq;
using Disqord;
using Disqord.Gateway;
using Unix.Data;

namespace Unix.Services.Extensions;

public static class UserExtensions
{
    public static ConcurrentDictionary<Snowflake, Snowflake> GuildModRoleIds = new();
    public static ConcurrentDictionary<Snowflake, Snowflake> GuildAdminRoleIds = new();

    public static bool CanUseCommands(this IMember member)
    {
        if (member.IsModerator() || member.IsAdmin() || member.GetGuild().OwnerId == member.Id)
        {
            return true;
        }
        using var unixContext = new UnixContext();
        var guildConfig = unixContext.GuildConfigurations
            .Find(member.GuildId);
        if (guildConfig == null)
        {
            return false;
        }
        if (member.RoleIds.Any())
        {
            if (guildConfig.RequiredRoleToUse != guildConfig.Id)
            {
                if (!member.RoleIds.Contains(guildConfig.RequiredRoleToUse))
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        else
        {
            if (guildConfig.RequiredRoleToUse != member.GuildId)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsModerator(this IMember member)
    {
        var roles = member.RoleIds.ToList();
        if (member.Id == member.GetGuild().OwnerId)
        {
            return true;
        }

        using (var db = new UnixContext())
        {
            var modRole = db.GuildConfigurations
                .Where(x => x.Id == member.GuildId)
                .Select(x => x.ModeratorRoleId)
                .SingleOrDefault();
            if (roles.Any())
            {
                if (roles.Contains(modRole))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static bool IsAdmin(this IMember member)
    {
        if (member.Id == member.GetGuild().OwnerId)
        {
            return true;

        }
        var roles = member.RoleIds.ToList();

        using (var db = new UnixContext())
        {
            var adminRole = db.GuildConfigurations
                .Where(x => x.Id == member.GuildId)
                .Select(x => x.AdministratorRoleId)
                .SingleOrDefault();
            if (roles.Any())
            {
                if (roles.Contains(adminRole))
                {
                    return true;
                }
            }

            return false;
        }
    }
}