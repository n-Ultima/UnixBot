using System;
using System.Collections.Generic;
using Disqord;

namespace Unix.Common;

public static class SnowflakeExtensions
{
    public static Snowflake[] ToSnowflakeArray(this ulong[] ulongArray)
    {
        List<Snowflake> Snowflakes = new();
        foreach (var entry in ulongArray)
        {
            if (Snowflake.TryParse(entry.ToString(), out Snowflake newFlake))
            {
                Snowflakes.Add(newFlake);
            }
            else
            {
                throw new Exception($"An entry provided could not be parsed as a valid snowflake: {entry}");
            }
        }
        return Snowflakes.ToArray();
    }
}