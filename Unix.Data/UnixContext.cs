using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Unix.Common;
using Unix.Data.Models.Core;
using Unix.Data.Models.Moderation;

namespace Unix.Data;

public class UnixContext : DbContext
{
    public UnixConfiguration UnixConfig = new();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var snowflakeConverter = new ValueConverter<Snowflake, ulong>(
            static snowflake => snowflake,
            static @ulong => new Snowflake(@ulong));
        modelBuilder.UseValueConverterForType<Snowflake>(snowflakeConverter);
        modelBuilder.Entity<GuildConfiguration>()
            .Property(x => x.WhitelistedInvites)
            .HasPostgresArrayConversion<ulong, decimal>(ulongs => Convert.ToDecimal(ulongs), decimals => Convert.ToUInt64(decimals));
        modelBuilder.Entity<GuildConfiguration>()
            .Property(x => x.SelfAssignableRoles)
            .HasPostgresArrayConversion<ulong, decimal>(ulongs => Convert.ToDecimal(ulongs), decimals => Convert.ToUInt64(decimals));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(UnixConfig.ConnectionString);
    }
    public DbSet<GuildConfiguration> GuildConfigurations { get; set; }

    public DbSet<Tag> Tags { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<Infraction> Infractions { get; set; }
    
    public DbSet<ReactionRole> ReactionRoles { get; set; }
}