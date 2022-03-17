using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Unix.Services.Core.Abstractions;
using Unix.Common;
using Unix.Services.Extensions;


namespace Unix.Modules.Bases;

public abstract class UnixAdminSlashCommandModuleBase : UnixSlashCommandModuleBase
{
    protected UnixAdminSlashCommandModuleBase(IGuildService guildService, ITagService tagService, IModerationService moderationService, IPhishermanService phishermanService, IOwnerService ownerService, IReminderService reminderService, IReactionRoleService reactionRoleService)
        : base(guildService, tagService, moderationService, phishermanService, ownerService, reminderService, reactionRoleService)
    {
        GuildService = guildService;
        TagService = tagService;
        ModerationService = moderationService;
        PhishermanService = phishermanService;
        OwnerService = ownerService;
        ReminderService = reminderService;
        ReactionRoleService = reactionRoleService;
    }


    public override void BeforeExecute(ICommandInfo command)
    {
        var guildUser = Context.User as IGuildUser;
        if (!guildUser.IsAdmin())
        {
            throw new Exception("Missing permissions.");
        }
    }
}