using System.Threading.Tasks;
using Disqord;

namespace Unix.Services.Core.Abstractions;

/// <summary>
///     Represents the Phisherman service, used to catch scam links.
/// </summary>
public interface IPhishermanService
{
    /// <summary>
    ///     Queries the Phisherman API for if the provided domain is marked as suspicious.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="domain">The domain to query.</param>
    /// <returns>A <see cref="bool"/> representing if the domain is suspicious.</returns>
    Task<bool> IsDomainSuspiciousAsync(Snowflake guildId, string domain);

    /// <summary>
    ///     Queries the Phisherman API for if the provided domain is marked as a verified phish.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="domain">The domain to query.</param>
    /// <returns>A <see cref="bool"/> representing if the domain is a verified phish.</returns>
    Task<bool> IsVerifiedPhishAsync(Snowflake guildId, string domain);

    /// <summary>
    ///     Reports a caught phish to the Phisherman API.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="domain">The caught domain.</param>
    /// <returns></returns>
    Task ReportCaughtPhishAsync(Snowflake guildId, string domain);
}