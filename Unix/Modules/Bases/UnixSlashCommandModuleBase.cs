using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules.Bases;

public abstract class UnixSlashCommandModuleBase : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    protected internal IGuildService GuildService { get; set; }
    protected internal ITagService TagService { get; set; }
    protected internal IModerationService ModerationService { get; set; }
    protected internal IPhishermanService PhishermanService { get; set; }
    protected internal IOwnerService OwnerService { get; set; }
    protected internal IReminderService ReminderService { get; set; }
    protected internal IReactionRoleService ReactionRoleService { get; set; }
    
    protected UnixSlashCommandModuleBase(IGuildService guildService, ITagService tagService, IModerationService moderationService, IPhishermanService phishermanService, IOwnerService ownerService, IReminderService reminderService, IReactionRoleService reactionRoleService)
    {
        GuildService = guildService;
        TagService = tagService;
        ModerationService = moderationService;
        PhishermanService = phishermanService;
        OwnerService = ownerService;
        ReminderService = reminderService;
        ReactionRoleService = reactionRoleService;
    }
    protected override async Task RespondAsync(string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, RequestOptions options = null, MessageComponent components = null, Embed embed = null)
    {
        await base.RespondAsync(text, embeds, isTTS, ephemeral, AllowedMentions.None, options, components, embed);
    }

    protected async Task SendSuccessAsync(string text)
    {
        await RespondAsync($"<:unixok:884524202458222662> {text}");
    }
}