using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Unix.Common;
using Unix.Data;
using Unix.Services.Core.Abstractions;
using Color = System.Drawing.Color;

namespace Unix.Services.Core;

public class ReminderExecutionHandler : UnixService
{
    private readonly IReminderService _reminderService;

    public ReminderExecutionHandler(IReminderService reminderService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _reminderService = reminderService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    remindLoop:
        while (true)
        {
            try
            {
                var reminders = await _reminderService.FetchRemindersAsync();
                if (!reminders.Any())
                {
                    await Task.Delay(30000);
                    continue;
                }

                var expiringReminder = reminders
                    .OrderBy(x => x.ExecutionTime)
                    .FirstOrDefault();
                if (expiringReminder.ExecutionTime <= DateTimeOffset.UtcNow)
                {
                    await Bot.SendMessageAsync(expiringReminder.ChannelId, new LocalMessage()
                        .WithContent($"{Mention.User(expiringReminder.UserId)} - reminder created on {Markdown.Timestamp(expiringReminder.CreatedAt)} | `{expiringReminder.Value}`"));
                    await _reminderService.DeleteReminderAsync(expiringReminder.Id);
                    await Task.Delay(30000);
                    continue;
                }
                else
                {
                    await Task.Delay(30000);
                    continue;
                }
            }
            catch
            {
                goto remindLoop;
            }
        }
    }
}