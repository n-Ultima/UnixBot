using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Unix.Data.Models.Core;

namespace Unix.Services.Core.Abstractions;

/// <summary>
///     Represents the reaction role service, used for handling reaction roles.
/// </summary>
public interface IReactionRoleService
{
    /// <summary>
    ///     Creates a reaction role.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <param name="emojiId">The ID of the emoji.</param>
    /// <returns></returns>
    Task CreateReactionRoleAsync(Snowflake guildId, Snowflake messageId, Snowflake emojiId, Snowflake roleId);

    /// <summary>
    ///     Fetches a reaction role.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="emojiId">The ID of the emoji/</param>
    /// <returns>A <see cref="ReactionRole"/></returns>
    Task<ReactionRole> FetchReactionRoleAsync(Snowflake guildId, Snowflake messageId, Snowflake emojiId);
    /// <summary>
    ///     Fetches a list of <see cref="ReactionRole"/> for the guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> that holds all of the guilds reaction roles.</returns>
    Task<IEnumerable<ReactionRole>> FetchReactionRolesAsync(Snowflake guildId);

    /// <summary>
    ///     Deletes a reaction role.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="messageId">The ID of the message that the reaction role is attached to.</param>
    /// <param name="emojiId">The ID of the emoji.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <returns></returns>
    Task DeleteReactionRoleAsync(Snowflake guildId, Snowflake messageId, Snowflake emojiId, Snowflake roleId);
}