using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Data.Models.Moderation;

namespace Unix.Services.Core.Abstractions;

/// <summary>
///     Represents the moderation service, used for handling infractions.
/// </summary>
public interface IModerationService
{
#nullable enable
    /// <summary>
    ///     Creates an infraction.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="moderatorId">The moderator ID creating the infraction.</param>
    /// <param name="subjectId">The subject ID receiving the infraction.</param>
    /// <param name="type">The <see cref="InfractionType"/> of the infraction.</param>
    /// <param name="reason">The reason for the infraction creation.</param>
    /// <param name="duration">The optional duration of the infraction.</param>
    /// <returns></returns>
    Task CreateInfractionAsync(Snowflake guildId, Snowflake moderatorId, Snowflake subjectId, InfractionType type, string reason, bool manual, TimeSpan? duration);

    /// <summary>
    ///     Fetches all infractions that have a duration.
    /// </summary>
    /// <returns>A <see cref="IEnumerable{Infraction}"/></returns>
    Task<IEnumerable<Infraction>> FetchTimedInfractionsAsync();

    /// <summary>
    ///     Updates the infraction's reason.
    /// </summary>
    /// <param name="infractionId">The ID of the infraction.</param>
    /// <param name="guildId">The ID of the guild that the infraction originated from.</param>
    /// <param name="newReason">The new reason to be applied.</param>
    /// <returns></returns>
    Task UpdateInfractionAsync(Guid infractionId, Snowflake guildId, string newReason);

    /// <summary>
    ///     Removes the infraction.
    /// </summary>
    /// <param name="infractionId">The ID of the infraction.</param>
    /// <param name="guildId">The guild ID that the infraction originated from.</param>
    /// <param name="removerId">The ID of the user who is removing the infraction.</param>
    /// <param name="removalMessage">The reason for removing the infraction.</param>
    /// <returns></returns>
    Task RemoveInfractionAsync(Guid infractionId, Snowflake guildId, Snowflake removerId, bool manual, string removalMessage);

    /// <summary>
    ///     Fetches the infraction provided.
    /// </summary>
    /// <param name="infractionId">The ID of the infraction.</param>
    /// <param name="guildId">The ID of the guild that the infraction originated from.</param>
    /// <returns>A <see cref="Infraction"/> with a matching ID.</returns>
    Task<Infraction> FetchInfractionAsync(Guid infractionId, Snowflake guildId);

    /// <summary>
    ///     Fetches a list of infractions for the user provided.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A <see cref="IEnumerable{Infraction}"/>.</returns>
    Task<IEnumerable<Infraction>> FetchInfractionsAsync(Snowflake guildId, Snowflake userId);

    /// <summary>
    ///     Logs an infraction creation.
    /// </summary>
    /// <param name="guild">The infraction's guild.</param>
    /// <param name="subject">The user receiving the infraction.</param>
    /// <param name="moderator">The user creating the infraction.</param>
    /// <param name="type">The <see cref="InfractionType"/> of infraction.</param>
    /// <param name="humanizedDuration">The optional duration of the infraction.</param>
    /// <param name="reason">The reason for the infraction.</param>
    /// <returns></returns>
    Task LogAsync(CachedGuild guild, IUser subject, IUser moderator, InfractionType type, string? humanizedDuration, string reason);

    /// <summary>
    ///     Logs an infraction deletion.
    /// </summary>
    /// <param name="infraction">The infraction being deleted.</param>
    /// <param name="infractionRemover">The user removing the infraction.</param>
    /// <param name="infractionSubject">The user who is the subject of the infraction.</param>
    /// <param name="reason">The reason for deleting the infraction.</param>
    /// <returns></returns>
    Task LogInfractionDeletionAsync(Infraction infraction, IUser infractionRemover, IUser infractionSubject, bool manual, string reason);
#nullable  enable

}