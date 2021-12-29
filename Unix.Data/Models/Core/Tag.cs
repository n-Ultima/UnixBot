using System.ComponentModel.DataAnnotations.Schema;
using Disqord;

namespace Unix.Data.Models.Core;

public class Tag
{

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public Snowflake GuildId { get; set; }

    public Snowflake OwnerId { get; set; }

    [Column(TypeName = "citext")]
    public string Name { get; set; }

    public string Content { get; set; }
}