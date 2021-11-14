using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Unix.Data
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder UseValueConverterForType<T>(this ModelBuilder modelBuilder, ValueConverter converter)
        {
            return modelBuilder.UseValueConverterForType(typeof(T), converter);
        }

        public static ModelBuilder UseValueConverterForType(this ModelBuilder modelBuilder, Type type, ValueConverter converter)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == type);
                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.Name).Property(property.Name)
                        .HasConversion(converter);
                }
            }

            return modelBuilder;
        }

        public static void ConfigureUlongListConverters(this ModelBuilder modelBuilder)
        {
            var ulongListConverter = new ValueConverter<List<ulong>, decimal[]>(ulongs => ulongs.Select(Convert.ToDecimal).ToArray(),
                decimals => decimals.Select(Convert.ToUInt64).ToList());
            foreach (var type in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in type.GetProperties())
                {
                    if (property.ClrType == typeof(List<ulong>))
                    {
                        property.SetValueConverter(ulongListConverter);
                        property.SetColumnType("numeric(20,0)[]");
                    }
                }
            }
        }
    }
}