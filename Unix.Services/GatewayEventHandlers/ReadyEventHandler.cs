using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Unix.Data;

namespace Unix.Services.GatewayEventHandlers
{
    public class ReadyEventHandler : UnixService
    {
        public ReadyEventHandler(IServiceProvider serviceProvider) : base(serviceProvider)
        {}

        protected override async ValueTask OnReady(ReadyEventArgs e)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
                var allowedGuildIds = await unixContext.GuildConfigurations.Select(x => x.Id).ToListAsync();
                var unauthorizedGuilds = e.GuildIds.Except(allowedGuildIds);
                Log.Logger.Warning("Guilds were found that Unix isn't authorized to operate in. IDs: [{guildIds}]", unauthorizedGuilds.Humanize());
                // Now, we leave each of the guilds that Unix shouldn't be in.
                foreach (var guild in unauthorizedGuilds)
                {
                    await Bot.LeaveGuildAsync(guild, new DefaultRestRequestOptions
                    {
                        Reason = "Unauthorized. Join the Unix server to request access."
                    });
                }
            }
        }
    }
}