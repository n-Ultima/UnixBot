using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Gateway.Default;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Unix.Common;
using Unix.Data;
using Unix.Services.Core;

namespace Unix;

class Startup
{
    private static readonly UnixConfiguration UnixConfig = new();
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}", theme: AnsiConsoleTheme.Code)
            .CreateLogger();
        var hostBuilder = new HostBuilder()
            .ConfigureAppConfiguration(x =>
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();
                x.AddConfiguration(config);
            })
            .ConfigureServices((_, services) =>
            {
                services
                    .AddSingleton<HttpClient>()
                    .AddDbContext<UnixContext>()
                    .Configure<DefaultGatewayCacheProviderConfiguration>(x => x.MessagesPerChannel = 200)
                    .AddUnixServices()
                    .AddCommands();
            })
            .ConfigureDiscordBot((_, bot) =>
            {
                bot.Intents = GatewayIntents.Bans |
                              GatewayIntents.Guilds |
                              GatewayIntents.Members |
                              GatewayIntents.EmojisAndStickers |
                              GatewayIntents.DirectMessages |
                              GatewayIntents.DirectReactions |
                              GatewayIntents.GuildReactions |
                              GatewayIntents.Webhooks |
                              GatewayIntents.GuildMessages |
                              GatewayIntents.MessageContent;
                bot.OwnerIds = UnixConfig.OwnerIds.ToSnowflakeArray();
                bot.Token = UnixConfig.Token;
                bot.ServiceAssemblies = new[]
                {
                    typeof(GuildService).Assembly,
                    typeof(UnixConfiguration).Assembly,
                    typeof(UnixBot).Assembly,
                    typeof(UnixContext).Assembly
                }.ToList();
                Log.Logger.ForContext<Startup>().Information("OwnerIds: {ownerIds}", bot.OwnerIds.Humanize());
            })
            .UseSerilog()
            .UseConsoleLifetime();
        using (var host = hostBuilder.Build())
        {
            using (var services = host.Services.CreateScope())
            {
                var db = services.ServiceProvider.GetRequiredService<UnixContext>();
                Log.Logger.ForContext<Startup>().Information("Applying migrations...");
                await db.Database.MigrateAsync();
                Log.Logger.ForContext<Startup>().Information("Migrations applied successfully!");
            }
            await host.RunAsync();
        }
    }
}