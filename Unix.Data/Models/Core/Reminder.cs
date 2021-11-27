using System;
using System.ComponentModel.DataAnnotations.Schema;
using Disqord;

namespace Unix.Data.Models.Core;

public class Reminder
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public Snowflake GuildId { get; set; }
    
    public Snowflake ChannelId { get; set; }
    
    public Snowflake UserId { get; set; }
    public string Value { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExecutionTime { get; set; }
}