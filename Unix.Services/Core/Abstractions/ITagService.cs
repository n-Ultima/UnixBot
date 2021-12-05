using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Unix.Data.Models.Core;

namespace Unix.Services.Core.Abstractions;

/// <summary>
///     Represents the tag service, used in handling tags.
/// </summary>
public interface ITagService
{
    Task CreateTagAsync(Snowflake guildId, Snowflake ownerId, string tagName, string tagContent);
    
    Task<Tag> FetchTagAsync(Snowflake guildId, string tagName);

    Task<IEnumerable<Tag>> FetchTagsAsync(Snowflake guildId);
    Task EditTagContentAsync(Snowflake guildId, Snowflake requestorId, string tagName, string tagContent);

    Task EditTagOwnershipAsync(Snowflake guildId, Snowflake requestorId, string tagName, Snowflake newOwnerId);

    Task DeleteTagAsync(Snowflake guildId, Snowflake requestorId, string tagName);
}