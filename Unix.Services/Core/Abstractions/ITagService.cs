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
    /// <summary>
    ///     Creates a tag.
    /// </summary>
    /// <param name="guildId">The ID of the guild ID that the tag will be created for.</param>
    /// <param name="ownerId">The ID of the user who owns the tag.</param>
    /// <param name="tagName">The name of the tag.</param>
    /// <param name="tagContent">The content that the tag should hold.</param>
    /// <returns></returns>
    Task CreateTagAsync(Snowflake guildId, Snowflake ownerId, string tagName, string tagContent);

    /// <summary>
    ///     Fetches a tag.
    /// </summary>
    /// <param name="guildId">The ID of the guild that the tag is associated with.</param>
    /// <param name="tagName">The name of the tag.</param>
    /// <returns>A <see cref="Tag"/> with a matching name and guild ID.</returns>
    Task<Tag> FetchTagAsync(Snowflake guildId, string tagName);

    /// <summary>
    ///     Fetches a list of tags for the guild provided ordered by name.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <returns>A <see cref="IEnumerable{Tag}"/> of the guild's tags.</returns>
    Task<IEnumerable<Tag>> FetchTagsAsync(Snowflake guildId);

    /// <summary>
    ///     Edits the content of a tag.
    /// </summary>
    /// <param name="guildId">The ID of the guild that the tag is associated with.</param>
    /// <param name="requestorId">The ID of the user requesting the edit.</param>
    /// <param name="tagName">The name of the tag.</param>
    /// <param name="tagContent">The new content that the tag should hold.</param>
    /// <returns></returns>
    Task EditTagContentAsync(Snowflake guildId, Snowflake requestorId, string tagName, string tagContent);

    /// <summary>
    ///     Transfers ownership of a tag to another user.
    /// </summary>
    /// <param name="guildId">The ID of the guild that the tag is associated with.</param>
    /// <param name="requestorId">The ID of the user requesting the transfer.</param>
    /// <param name="tagName">The name of the tag.</param>
    /// <param name="newOwnerId">The new owner's ID.</param>
    /// <returns></returns>
    Task EditTagOwnershipAsync(Snowflake guildId, Snowflake requestorId, string tagName, Snowflake newOwnerId);

    /// <summary>
    ///     Deletes a tag.
    /// </summary>
    /// <param name="guildId">The ID of the guild that the tag is associated with.</param>
    /// <param name="requestorId">The ID of the user requesting the deletion.</param>
    /// <param name="tagName">The name of the tag.</param>
    /// <returns></returns>
    Task DeleteTagAsync(Snowflake guildId, Snowflake requestorId, string tagName);
}