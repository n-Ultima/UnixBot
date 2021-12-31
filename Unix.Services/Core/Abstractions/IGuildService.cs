using System.Threading.Tasks;
using Disqord;
using Unix.Data.Models.Core;

namespace Unix.Services.Core.Abstractions;

/// <summary>
///     Represents the guild service, used to modify and handle guild configurations.
/// </summary>
public interface IGuildService
{
    /// <summary>
    ///     Fetches the guild configuration for the guild ID provided.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <returns>A <see cref="GuildConfiguration"/>.</returns>
    Task<GuildConfiguration> FetchGuildConfigurationAsync(Snowflake guildId);

    /// <summary>
    ///     Modifies the guild's mod log channel ID.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="modLogChannelId">The ID of the mod log channel.</param>
    /// <returns></returns>
    Task ModifyGuildModLogChannelIdAsync(Snowflake guildId, Snowflake modLogChannelId);

    /// <summary>
    ///     Modifies the guild's message log channel ID.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="messageLogChannelId">The ID of the message log channel.</param>
    /// <returns></returns>
    Task ModifyGuildMessageLogChannelIdAsync(Snowflake guildId, Snowflake messageLogChannelId);

    /// <summary>
    ///     Modifies the guild's automoderation to on or off.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="automodEnabled">Whether automod should be enabled.</param>
    /// <returns></returns>
    Task ModifyGuildAutomodAsync(Snowflake guildId, bool automodEnabled);

    /// <summary>
    ///     Modifies the guild's moderator role ID.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="modRoleId">The ID of the moderator role.</param>
    /// <returns></returns>
    Task ModifyGuildModRoleAsync(Snowflake guildId, Snowflake modRoleId);

    /// <summary>
    ///     Modifies the guild's miscellaneous log channel ID.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="miscChannelId">The ID of the miscellaneous log channel.</param>
    /// <returns></returns>
    Task ModifyGuildMiscellaneousLogChannelIdAsync(Snowflake guildId, Snowflake miscChannelId);

    /// <summary>
    ///     Modifies the guild's administrator role ID.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="adminRoleId">The ID of the administrator role.</param>
    /// <returns></returns>
    Task ModifyGuildAdminRoleAsync(Snowflake guildId, Snowflake adminRoleId);

    /// <summary>
    ///     Modifies the guild's required role ID(the lowest role required for Unix to listen to commands).
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="requiredRoleId">The ID of the required role.</param>
    /// <returns></returns>
    Task ModifyGuildRequiredRoleAsync(Snowflake guildId, Snowflake requiredRoleId);

    /// <summary>
    ///     Modifies the guild's Phisherman API key.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="apiKey">The Phisherman API key.</param>
    /// <returns></returns>
    /// <remarks>Learn how to get a key from https://docs.phisherman.gg</remarks>
    Task ModifyGuildPhishermanApiKeyAsync(Snowflake guildId, string apiKey);

    /// <summary>
    ///     Adds a banned term to the guild's list of banned terms.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="bannedTerm">The term to ban.</param>
    /// <returns></returns>
    Task AddBannedTermAsync(Snowflake guildId, string bannedTerm);

    /// <summary>
    ///     Removes a banned term from the guild's list of banned terms.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="bannedTerm">The term to unban.</param>
    /// <returns></returns>
    Task RemoveBannedTermAsync(Snowflake guildId, string bannedTerm);

    /// <summary>
    ///     Adds the guild ID to the list of allowed guilds. Invites sent that point to the provided guild won't be deleted.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="inviteGuildId">The ID of the guild to be whitelisted.</param>
    /// <returns></returns>
    Task AddWhitelistedInviteAsync(Snowflake guildId, Snowflake inviteGuildId);

    /// <summary>
    ///     Removes the guild ID from the list of allowed guilds. Invites sent that point to the provided guild will be deleted.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="inviteGuildId">The ID of the guild to remove from the whitelist.</param>
    /// <returns></returns>
    Task RemoveWhitelistedInviteAsync(Snowflake guildId, Snowflake inviteGuildId);

    /// <summary>
    ///     Adds a self assignable role to the guild's list of self assignable roles.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <returns></returns>
    Task AddSelfAssignableRoleAsync(Snowflake guildId, Snowflake roleId);

    /// <summary>
    ///     Removes the role ID provided from the guild's list of self assignable roles.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <returns></returns>
    Task RemoveSelfAssignableRoleAsync(Snowflake guildId, Snowflake roleId);

    /// <summary>
    ///     Adds the role ID to the guild's list of autoroles.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <returns></returns>
    Task AddAutoRoleAsync(Snowflake guildId, Snowflake roleId);
    
    /// <summary>
    ///     Removes the role ID from the guild's list of autoroles.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <returns></returns>
    Task RemoveAutoRoleAsync(Snowflake guildId, Snowflake roleId);
}