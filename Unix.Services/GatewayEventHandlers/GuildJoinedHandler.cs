using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Unix.Common;
using Unix.Data;
using Unix.Data.Models.Core;

namespace Unix.Services.GatewayEventHandlers;

public class GuildJoinedHandler : UnixService
{
    private UnixConfiguration UnixConfig = new();
    public GuildJoinedHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override async ValueTask OnJoinedGuild(JoinedGuildEventArgs e)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(e.GuildId);
            if (guildConfig == null)
            {
                if (UnixConfig.PrivelegedMode)
                {
                    return;
                }
                var firstChannel = e.Guild.Channels
                    .Select(x => x.Value)
                    .Where(x => x.Type == ChannelType.Text)
                    .First();
                try
                {
                    await (firstChannel as ITextChannel).SendMessageAsync(new LocalMessage()
                        .WithContent("Thank you for adding Unix! A few things to note:\n1. Join the Unix Discord Server for support and bug issues(http://www.ultima.one/unix)\n2. Use the `/setup-guild` command to setup your guild(only the owner can do this).\n3. If this guild is found to be in violation of Discord's TOS or any other applicable laws, the guild will be blacklisted from using Unix.\n4. Please run commands to setup your guild fully! All of these commands start with the `/configure` prefix, such as `/configure-modrole`."));
                }
                catch (RestApiException)
                {
                    await Bot.SendMessageAsync(e.Guild.OwnerId, new LocalMessage()
                        .WithContent("Thank you for adding Unix! A few things to note:\n1. Join the Unix Discord Server for support and bug issues(http://www.ultima.one/unix)\n2. Use the `/configure-modrole` and `/configure-adminrole` to setup permissions. Until this is done only the owner can perform moderation commands.\n3. If this guild is found to be in violation of Discord's TOS or any other applicable laws, the guild will be blacklisted from using Unix.\n4. Please run commands to setup your guild fully! All of these commands start with the `/configure` prefix, such as `/configure-modrole`."));
                }

                unixContext.GuildConfigurations.Add(new GuildConfiguration
                {
                    Id = e.GuildId
                });
                await unixContext.SaveChangesAsync();
            }
        }
    }
}