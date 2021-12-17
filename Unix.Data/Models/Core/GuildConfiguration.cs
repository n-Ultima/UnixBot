using System.Collections.Generic;
using Disqord;

namespace Unix.Data.Models.Core;

public class GuildConfiguration
{
    public Snowflake Id { get; set; }

    public Snowflake ModLogChannelId { get; set; }

    public Snowflake ModeratorRoleId { get; set; }

    public Snowflake AdministratorRoleId { get; set; }

    public Snowflake RequiredRoleToUse { get; set; }
    public Snowflake MessageLogChannelId { get; set; }

    public Snowflake MuteRoleId { get; set; }

    public bool AutomodEnabled { get; set; }

    public List<string> BannedTerms { get; set; } = new();

    public List<ulong> WhitelistedInvites { get; set; } = new();

    public List<ulong> SelfAssignableRoles { get; set; } = new();
    public int AmountOfMessagesConsideredSpam { get; set; }

    public string PhishermanApiKey { get; set; }
}