using System;
using Disqord;

namespace Unix.Data.Models.Moderation;

public class Infraction
{
    public Guid Id { get; set; }
    public Snowflake GuildId { get; set; }
    public Snowflake ModeratorId { get; set; }
    public Snowflake SubjectId { get; set; }
    public TimeSpan? Duration { get; set; } = null;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; } = null;
    public string Reason { get; set; }
    public InfractionType Type { get; set; }

    public bool IsRescinded { get; set; } = false;
}