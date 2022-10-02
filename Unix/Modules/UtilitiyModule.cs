using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Qmmands;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;
using Unix.Services.Extensions;

namespace Unix.Modules;

public class UtilitiyModule : UnixModuleBase
{
    public UtilitiyModule(IGuildService guildConfigurationService, IModerationService moderationService) : base(guildConfigurationService)
    {
    }

    [SlashCommand("ping")]
    [Description("Find out.")]
    public IResult Ping()
    {
        return Response("Pong!");
    }

    [SlashCommand("info")]
    [Description("Fetches info about a user.")]
    public async Task<IResult> FetchUserInfoAsync(IMember member = null)
    {
        if (member is null)
        {
            member = Context.Author;
        }
        var userInfoEmbed = new LocalEmbed()
            .WithTitle(member.Tag)
            .WithAuthor(member)
            .WithThumbnailUrl(member.GetAvatarUrl() ?? member.GetDefaultAvatarUrl())
            .AddField("ID", member.Id)
            .AddField("Joined", $"{Markdown.Timestamp(member.JoinedAt.Value)}({(DateTimeOffset.UtcNow - member.JoinedAt.Value).Humanize()} ago.)")
            .AddField("Created", $"{Markdown.Timestamp(member.CreatedAt())}({(DateTimeOffset.UtcNow - member.CreatedAt()).Humanize()} ago.)")
            .AddField("Roles", member.GetRoles().Select(x => x.Value.Name).Humanize())
            .AddField("Hierarchy", Context.Author.GetGuild().OwnerId == member.Id ? "Guild Owner" : member.CalculateRoleHierarchy())
            .WithColor(Color.Gold);
        return Response(userInfoEmbed);
    }

    [SlashCommand("botinfo")]
    [Description("Fetches info about Unix.")]
    public async Task<IResult> BotInfoAsync()
    {
        var botInfoEmbed = new LocalEmbed()
            .WithTitle("Unix Info")
            .WithAuthor(Bot.CurrentUser)
            .WithColor(Color.Purple)
            .WithDescription("Unix is a multi feature Discord bot that boasts reliability, efficiency, and simplicity. You can vote for the bot on [top.gg](https://top.gg/bot/817577290057383947).")
            .AddField("Developer(s)", "Ultima#2000", true)
            .AddField("Contributor(s)", "Voxel#8113", true)
            .AddField("Github", "https://www.github.com/n-Ultima/UnixBot")
            .AddField("Library", "Disqord", true)
            .AddField("Language", "C#", true)
            .AddField("Support Server", "will impl " /* TODO: get unix server back up and fix this */, true);
        return Response(botInfoEmbed);
    }
}