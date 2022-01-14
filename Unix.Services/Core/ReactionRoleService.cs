using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Unix.Data;
using Unix.Data.Models.Core;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.Core;

public class ReactionRoleService : UnixService, IReactionRoleService
{
    public ReactionRoleService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <inheritdoc />
    public async Task CreateReactionRoleAsync(Snowflake guildId, Snowflake messageId, Snowflake emojiId, Snowflake roleId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var reactionRole = await unixContext.ReactionRoles
                .Where(x => x.GuildId == guildId)
                .Where(x => x.MessageId == messageId)
                .Where(x => x.EmojiId == emojiId)
                .Where(x => x.RoleId == roleId)
                .SingleOrDefaultAsync();
            if (reactionRole != null)
            {
                throw new Exception("Reaction role with the provided data already exists.");
            }

            unixContext.ReactionRoles.Add(new ReactionRole {GuildId = guildId, MessageId = messageId, EmojiId = emojiId, RoleId = roleId});
            await unixContext.SaveChangesAsync();
        }
    }
    
    /// <inheritdoc />
    public async Task<ReactionRole> FetchReactionRoleAsync(Snowflake guildId, Snowflake messageId, Snowflake emojiId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.ReactionRoles
                .Where(x => x.GuildId == guildId)
                .Where(x => x.MessageId == messageId)
                .Where(x => x.EmojiId == emojiId)
                .SingleOrDefaultAsync();
        }
    }

    /// <inheritdoc />
    public async Task<ReactionRole> FetchReactionRoleAsync(long id)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.ReactionRoles
                .FindAsync(id);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReactionRole>> FetchReactionRolesAsync(Snowflake guildId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var roles = await unixContext.ReactionRoles
                .Where(x => x.GuildId == guildId)
                .ToListAsync();
            return roles;
        }    
    }

    /// <inheritdoc />
    public async Task DeleteReactionRoleAsync(Snowflake guildId, Snowflake messageId, Snowflake emojiId, Snowflake roleId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var reactionRole = await unixContext.ReactionRoles
                .Where(x => x.GuildId == guildId)
                .Where(x => x.MessageId == messageId)
                .Where(x => x.EmojiId == emojiId)
                .Where(x => x.RoleId == roleId)
                .SingleOrDefaultAsync();
            if (reactionRole == null)
            {
                throw new Exception("Reaction role with the provided data does not exist.");
            }

            unixContext.ReactionRoles.Remove(reactionRole);
            await unixContext.SaveChangesAsync();
        }    
    }
}