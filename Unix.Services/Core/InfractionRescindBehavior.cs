using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unix.Data.Models.Moderation;

namespace Unix.Services.Core
{
    public class InfractionRescindBehavior : UnixService
    {
        private readonly ModerationService _moderationService;
        
        public InfractionRescindBehavior(IServiceProvider serviceProvider, ModerationService moderationService) : base(serviceProvider)
        {
            _moderationService = moderationService;
        }

        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
        /// <summary>
        ///     Method used for rescinding infractions with a duration. We check every 30 seconds for expiring infractions.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await Bot.WaitUntilReadyAsync(ct);
            whileLoop:
            while (true)
            {
                try
                {
                    var timedInfractions = await _moderationService.FetchTimedInfractionsAsync();
                    if (!timedInfractions.Any())
                    {
                        await Task.Delay(Interval);
                        continue;
                    }

                    var expiringInfraction = timedInfractions
                        .OrderBy(x => x.ExpiresAt)
                        .FirstOrDefault();
                    if (expiringInfraction.CreatedAt + expiringInfraction.Duration <= DateTimeOffset.UtcNow)
                    {
                        string reason = null;
                        switch (expiringInfraction.Type)
                        {
                            case InfractionType.Ban:
                                reason = "Temporary ban expired.";
                                break;
                            case InfractionType.Mute:
                                reason = "Mute expired.";
                                break;
                        }
                    
                        await _moderationService.RemoveInfractionAsync(expiringInfraction.Id, expiringInfraction.GuildId, reason);
                    }

                    await Task.Delay(Interval);
                    continue;
                }
                catch
                {
                    goto whileLoop;
                }
            }
            
        }
    }
}