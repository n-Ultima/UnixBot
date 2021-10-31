using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Unix.Common;
using Unix.Data;
using Unix.Modules;
using Unix.Modules.Bases;
using Unix.Services.Core;

namespace Unix
{
    class Startup
    {
        private static UnixConfiguration UnixConfig = new();
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
                        .AddPrefixProvider<UnixPrefixProvider>()
                        .AddDbContext<UnixContext>()
                        .AddCommands();
                })
                .ConfigureDiscordBotSharder<UnixBot>((_, bot) =>
                {
                    var ownerIds = ParseUlongArray(UnixConfig.OwnerIds);
                    bot.Intents = GatewayIntent.Bans |
                                  GatewayIntent.Guilds |
                                  GatewayIntent.Members |
                                  GatewayIntent.EmojisAndStickers |
                                  GatewayIntent.DirectMessages |
                                  GatewayIntent.DirectReactions |
                                  GatewayIntent.GuildReactions |
                                  GatewayIntent.Webhooks |
                                  GatewayIntent.GuildMessages;
                    bot.OwnerIds = ownerIds;
                    bot.Token = UnixConfig.Token;
                    bot.ServiceAssemblies = new[]
                    {
                        typeof(GuildService).Assembly,
                        typeof(UnixConfiguration).Assembly,
                        typeof(UnixGuildModuleBase).Assembly,
                        typeof(UnixContext).Assembly
                    }.ToList();
                    Log.Logger.Information("OwnerIds: {ownerIds}", bot.OwnerIds.Humanize());
                })
                .UseSerilog()
                .UseConsoleLifetime();
            using (var host = hostBuilder.Build())
            {
                await host.RunAsync();
            }
        }

        static Snowflake[] ParseUlongArray(ulong[] ulongs)
        {
            List<Snowflake> Snowflakes = new();
            foreach(var entry in ulongs)
            {
                if (Snowflake.TryParse(entry.ToString(), out Snowflake newFlake))
                {
                    Snowflakes.Add(newFlake);
                }
            }

            return Snowflakes.ToArray();
        }
    }
}