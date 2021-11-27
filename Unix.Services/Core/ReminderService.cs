using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Unix.Common;
using Unix.Data;
using Unix.Data.Models.Core;

namespace Unix.Services.Core;

public class ReminderService : UnixService
{
    public ReminderService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task CreateReminderAsync(Snowflake guildId, Snowflake channelId, Snowflake userId, TimeSpan duration, string reminderMessage)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var now = DateTimeOffset.UtcNow;
            unixContext.Reminders.Add(new Reminder
            {
                GuildId = guildId,
                ChannelId = channelId,
                UserId = userId,
                ExecutionTime = now + duration,
                CreatedAt = now,
                Value = reminderMessage
            });
            await unixContext.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Reminder>> FetchRemindersAsync(Snowflake guildId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.Reminders
                .Where(x => x.GuildId == guildId)
                .ToListAsync();
        }
    }
    
    public async Task<IEnumerable<Reminder>> FetchRemindersAsync()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.Reminders.ToListAsync();
        }
    }

    public async Task<IEnumerable<Reminder>> FetchRemindersForUserAsync(Snowflake guildId, Snowflake userId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.Reminders
                .Where(x => x.GuildId == guildId)
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }
    }

    public async Task<Reminder> FetchReminderAsync(long id)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.Reminders
                .FindAsync(id);
        }
    }

    public async Task DeleteReminderAsync(long id)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var reminder = await unixContext.Reminders
                .FindAsync(id);
            if (reminder == null)
            {
                throw new Exception("Invalid reminder ID.");
            }

            unixContext.Reminders.Remove(reminder);
            await unixContext.SaveChangesAsync();
        }
    }
}