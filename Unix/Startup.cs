using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Serilog;
using Unix.Services.Core.Abstractions;

namespace Unix;

public class Startup : DiscordShardedClientService
{
    private readonly IGuildService _guildService;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordSocketClient _client;
    public Startup(DiscordShardedClient sclient, ILogger<DiscordShardedClientService> logger, IGuildService guildService, InteractionService interactionService, IServiceProvider serviceProvider, DiscordSocketClient client) : base(sclient, logger)
    {
        _guildService = guildService;
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
        _client = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
#if  DEBUG
        await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);
#endif
        Logger.LogInformation("Starting up");
        Client.ShardReady += OnShardReady;
        Client.InteractionCreated += OnInteractionReceived; 
        _interactionService.SlashCommandExecuted += OnSlashCommandExecuted;
    }

    private async Task OnSlashCommandExecuted(SlashCommandInfo command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            await context.Interaction.RespondAsync($"⚠ {result.ErrorReason}");
        }
    }


    private async Task OnInteractionReceived(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(_client, interaction);
        await _interactionService.ExecuteCommandAsync(context, _serviceProvider);
    }

    private async Task OnShardReady(DiscordSocketClient arg)
    {
        foreach (var guild in arg.Guilds)
        {
            var config = await _guildService.FetchGuildConfigurationAsync(guild.Id);
            if (config == null)
            {
                Logger.LogInformation("Found guild with no existing configuration({id}. Creating...", guild.Id);
                await _guildService.CreateGuildConfigurationAsync(guild.Id);
                Logger.LogInformation("Configuration created successfully.");
            }
        }
    }
}