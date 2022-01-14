using System.ComponentModel.DataAnnotations.Schema;
using Disqord;

namespace Unix.Data.Models.Core;

public class ReactionRole
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public Snowflake GuildId { get; set; }

    public Snowflake MessageId { get; set; }

    public Snowflake RoleId { get; set; }
    public Snowflake EmojiId { get; set; }
}