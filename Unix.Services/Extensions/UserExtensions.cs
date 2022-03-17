using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.Serialization;
using Discord;
using Disqord;
using Disqord.Gateway;
using Unix.Common;
using Unix.Data;

namespace Unix.Services.Extensions;

public static class UserExtensions
{
    public static UnixConfiguration UnixConfig = new();
    public static ConcurrentDictionary<Snowflake, Snowflake> GuildModRoleIds = new();
    public static ConcurrentDictionary<Snowflake, Snowflake> GuildAdminRoleIds = new();

    public static bool CanUseCommands(this IGuildUser user)
    {
        if (user.IsModerator() || user.IsAdmin() || user.Guild.OwnerId == user.Id)
        {
            return true;
        }

        using var unixContext = new UnixContext();
        var guildConfig = unixContext.GuildConfigurations
            .Find(user.GuildId);
        if (guildConfig == null)
        {
            if (UnixConfig.PrivelegedMode)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (user.RoleIds.Any())
        {
            if (guildConfig.RequiredRoleToUse == default)
            {
                return true;
            }
            if (guildConfig.RequiredRoleToUse != guildConfig.Id)
            {
                if (!user.RoleIds.Contains(guildConfig.RequiredRoleToUse))
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
            if (guildConfig.RequiredRoleToUse != user.GuildId)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsModerator(this IGuildUser user)
    {
        var roles = user.RoleIds.ToList();
        if (user.Id == user.Guild.OwnerId)
        {
            return true;
        }

        using (var db = new UnixContext())
        {
            var modRole = db.GuildConfigurations
                .Where(x => x.Id == user.GuildId)
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

    public static bool IsAdmin(this IGuildUser user)
    {
        if (user.Id == user.Guild.OwnerId)
        {
            return true;

        }
        var roles = user.RoleIds.ToList();

        using (var db = new UnixContext())
        {
            var adminRole = db.GuildConfigurations
                .Where(x => x.Id == user.GuildId)
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
            if (UnixConfig.PrivelegedMode)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        if (member.RoleIds.Any())
        {
            if (guildConfig.RequiredRoleToUse == default)
            {
                return true;
            }
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