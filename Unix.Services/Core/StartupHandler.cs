using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Unix.Data;
using Unix.Data.Models.Core;

namespace Unix.Services.Core;

public class StartupHandler : UnixService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    public StartupHandler(IHostApplicationLifetime applicationLifetime, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _applicationLifetime = applicationLifetime;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var whitelistedGuilds = await unixContext.GuildConfigurations.Select(x => x.Id).ToListAsync();
            OwnerService.WhitelistedGuilds.AddRange(whitelistedGuilds);
        }
    }
}