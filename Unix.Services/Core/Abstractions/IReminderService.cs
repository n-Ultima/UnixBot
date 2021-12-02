using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Unix.Data.Models.Core;

namespace Unix.Services.Core.Abstractions;

public interface IReminderService
{
    /// <summary>
    ///     Creates a reminder.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The creator's ID.</param>
    /// <param name="duration">The duration until the reminder should execute.</param>
    /// <param name="reminderMessage">The message to deliver once the <see cref="duration"/> has passed.</param>
    /// <returns></returns>
    Task CreateReminderAsync(Snowflake guildId, Snowflake channelId, Snowflake userId, TimeSpan duration, string reminderMessage);

    /// <summary>
    ///     Fetches a list of ongoing reminders.
    /// </summary>
    /// <returns>A <see cref="IEnumerable{Reminder}"/>.</returns>
    Task<IEnumerable<Reminder>> FetchRemindersAsync();

    /// <summary>
    ///     Fetches a list of reminders for the user provided.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The user's ID.</param>
    /// <returns></returns>
    Task<IEnumerable<Reminder>> FetchRemindersForUserAsync(Snowflake guildId, Snowflake userId);

    /// <summary>
    ///     Fetches a reminder.
    /// </summary>
    /// <param name="id">The ID of the reminder.</param>
    /// <returns>A <see cref="Reminder"/> with a matching ID.</returns>
    Task<Reminder> FetchReminderAsync(long id);

    /// <summary>
    ///     Deletes a reminder.
    /// </summary>
    /// <param name="id">The ID of the reminder.</param>
    /// <returns></returns>
    Task DeleteReminderAsync(long id);
}