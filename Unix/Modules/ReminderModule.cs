using System;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;
using Unix.Common;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;
using Unix.Services.Parsers;

namespace Unix.Modules;

public class ReminderModule : UnixModuleBase
{
    private readonly IReminderService _reminderService;
    public ReminderModule(IGuildService guildConfigurationService, IReminderService reminderService) : base(guildConfigurationService)
    {
        _reminderService = reminderService;
    }

    [SlashCommand("reminders")]
    [Description("Fetches a list of reminders that you currently have.")]
    public async Task<IResult> FetchRemindersAsync()
    {
        var currentReminders = await _reminderService.FetchRemindersForUserAsync(Context.GuildId, Context.AuthorId);
        if (!currentReminders.Any())
        {
            await Context.SendEphmeralErrorAsync("You have no current reminders.");
            return null;
        }
        var currentRemindersEmbed = new LocalEmbed()
            .WithColor(System.Drawing.Color.Gold);
        foreach (var reminder in currentReminders.OrderBy(x => x.ExecutionTime))
        {
            currentRemindersEmbed.AddField($"Reminder {reminder.Id}", $"{Markdown.Timestamp(reminder.ExecutionTime)} - `{reminder.Value}`");
        }

        return Response(currentRemindersEmbed);
    }

    [SlashCommand("remind")]
    [Description("Sets a reminder for you.")]
    public async Task<IResult> CreateReminderAsync(string timeSpan, string message)
    {
        if (!TimeSpanParser.TryParseTimeSpan(timeSpan, out var reminderTimeSpan))
        {
            await Context.SendEphmeralErrorAsync("The timespan provided is not valid(e.g. 2 hours, 5 minutes).");
            return null;
        }

        try
        {
            await _reminderService.CreateReminderAsync(Context.GuildId, Context.ChannelId, Context.AuthorId, reminderTimeSpan, message);
            return Success("Successfully created reminder.");
        }
        catch (Exception e)
        {
            return EphmeralFailure(e.Message);
        }
    }
}