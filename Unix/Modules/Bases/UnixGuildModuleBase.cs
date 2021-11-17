using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Unix.Common;
using Unix.Data;
using Unix.Data.Models.Core;
using Unix.Services;
using Unix.Services.Core;

namespace Unix.Modules.Bases
{
    public class UnixGuildModuleBase : DiscordGuildModuleBase
    {
        private UnixConfiguration UnixConfig = new();
        private Dictionary<CachedGuild, GuildConfiguration> GuildConfigurations = new();
        public GuildConfiguration CurrentGuildConfiguration { get; set; }

        /// <summary>
        ///     Replies with a message that contains the <:unixok:884524202458222662> emoji along with the message provided.
        /// </summary>
        /// <param name="content">The content to display along with the unixok emoji.</param>
        /// <returns>A <see cref="DiscordCommandResult"/> showing success.</returns>
        protected DiscordCommandResult Success(string content)
        {
            var builder = new StringBuilder()
                .Append($"<:unixok:884524202458222662> {content}");
            return Response(new LocalMessage()
                .WithContent(builder.ToString()));
        }

        /// <summary>
        ///     Replies with a message that contains the ⚠ emoji along with the message provided.
        /// </summary>
        /// <param name="content">The content to be displayed along with the warning emoji.</param>
        /// <returns>A <see cref="DiscordCommandResult"/> showing failure.</returns>
        protected DiscordCommandResult Failure(string content)
        {
            var builder = new StringBuilder()
                .Append($"⚠ {content}");
            return Response(new LocalMessage()
                .WithContent(builder.ToString()));
        }

        protected async ValueTask<bool> PromptAsync(LocalMessage message = null)
        {
            var view = new PromptView(message ?? new LocalMessage().WithContent("Do you want to proceed?"));
            await View(view);
            return view.Result;
        }

        private sealed class PromptView : ViewBase
        {
            public bool Result { get; private set; }

            public PromptView(LocalMessage message)
                : base(message)
            { }

            [Button(Label = "Confirm", Style = LocalButtonComponentStyle.Success)]
            public async ValueTask Confirm(ButtonEventArgs e)
                => await HandleAsync(true, e);

            [Button(Label = "Cancel", Style = LocalButtonComponentStyle.Danger)]
            public async ValueTask Deny(ButtonEventArgs e)
                => await HandleAsync(false, e);

            private async ValueTask HandleAsync(bool result, ButtonEventArgs e)
            {
                Result = result;
                var message = (Menu as DefaultMenu).Message;
                _ = result
                    ? await message.ModifyAsync(x =>
                    {
                        x.Content = "Confirmation received, continuing the operation.";
                        x.Embeds = Array.Empty<LocalEmbed>();
                        x.Components = Array.Empty<LocalRowComponent>();
                    })
                    : await message.ModifyAsync(x =>
                    {
                        x.Content = "Cancellation received, cancelling the operation.";
                        x.Embeds = Array.Empty<LocalEmbed>();
                        x.Components = Array.Empty<LocalRowComponent>();
                    });
                Menu.Stop();
                return;
            }
        }
        protected override async ValueTask BeforeExecutedAsync()
        {
            using (var scope = Context.Bot.Services.CreateScope())
            {
                var guildService = scope.ServiceProvider.GetRequiredService<GuildService>();
                if (!GuildConfigurations.ContainsKey(Context.Guild))
                {
                    var guildConfiguration = await guildService.FetchGuildConfigurationAsync(Context.GuildId);
                    GuildConfigurations.Add(Context.Guild, guildConfiguration);
                }
            }
            var config = GuildConfigurations[Context.Guild];

            if (UnixConfig.OwnerIds.Contains(Context.Author.Id) || OwnerService.WhitelistedGuilds.Contains(Context.GuildId) || Context.Author.RoleIds.Contains(config.RequiredRoleToUse))
            {
                CurrentGuildConfiguration = config;
            }
            throw new Exception("Blacklisted.");
        }
    }
}