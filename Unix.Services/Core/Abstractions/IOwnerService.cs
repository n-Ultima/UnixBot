using System.Threading.Tasks;
using Disqord;

namespace Unix.Services.Core.Abstractions;

/// <summary>
///     Represents the owner service, used for the owner of Unix to manage the bot.
/// </summary>
public interface IOwnerService
{
    /// <summary>
    ///   Configures a guild for use with Unix. 
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="modLogChannelId">The ID of the guild's mod log channel.</param>
    /// <param name="messageLogChannelId">The ID of the guild's message log channel.</param>
    /// <param name="modRoleId">The ID of the guild's moderator role.</param>
    /// <param name="adminRoleId">The ID of the guild's administrator role.</param>
    /// <param name="automodEnabled">Whether to automoderate the guild.</param>
    /// <returns></returns>
    Task ConfigureGuildAsync(Snowflake guildId, Snowflake modLogChannelId, Snowflake messageLogChannelId, Snowflake modRoleId, Snowflake adminRoleId, bool automodEnabled);

    /// <summary>
    ///   Blacklists a guild from using Unix.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <returns></returns>
    Task BlacklistGuildAsync(Snowflake guildId);
}