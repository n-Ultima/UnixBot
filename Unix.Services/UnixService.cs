using System;
using Disqord.Bot.Hosting;

namespace Unix.Services
{
    public class UnixService : DiscordBotService
    {
        internal protected IServiceProvider ServiceProvider;

        public UnixService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
    }
}