using System.Threading.Tasks;
using Disqord;

namespace Unix.Services.Core.Abstractions;

/// <summary>
///     Represents the reaction role service, used for handling reaction roles.
/// </summary>
public interface IReactionRoleService
{
    Task CreateReactionRoleAsync(Snowflake guildId, Snowflake messageId, Snowflake roleId, string emoji);
    
    Task DeleteReactionRoleAsync(Snowflake guildId, Snowflake messageId, Snowflake roleId);
}