using System.Collections.Generic;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Unix.Common;
using Unix.Data.Models.Core;
using Unix.Data.Models.Moderation;

namespace Unix.Data
{
    public class UnixContext : DbContext
    {
        public UnixConfiguration UnixConfig = new();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var snowflakeConverter = new ValueConverter<Snowflake, ulong>(
                static snowflake => snowflake,
                static @ulong => new Snowflake(@ulong));
            modelBuilder.UseValueConverterForType<Snowflake>(snowflakeConverter);
            modelBuilder.ConfigureUlongListConverters();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(UnixConfig.ConnectionString);
        }
        public DbSet<GuildConfiguration> GuildConfigurations { get; set; }

        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<Infraction> Infractions { get; set; }
    }
}