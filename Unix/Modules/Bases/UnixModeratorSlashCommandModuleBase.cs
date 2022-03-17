using System;
using Discord;
using Discord.Interactions;
using Unix.Services.Core.Abstractions;
using Unix.Services.Extensions;

namespace Unix.Modules.Bases;

public abstract class UnixModeratorSlashCommandModuleBase : UnixSlashCommandModuleBase
{
    protected UnixModeratorSlashCommandModuleBase(IGuildService guildService, ITagService tagService, IModerationService moderationService, IPhishermanService phishermanService, IOwnerService ownerService, IReminderService reminderService, IReactionRoleService reactionRoleService) 
        : base(guildService, tagService, moderationService, phishermanService, ownerService, reminderService, reactionRoleService)
    {
    }
    
    public override void BeforeExecute(ICommandInfo command)
    {
        var guildUser = Context.User as IGuildUser;
        if (!guildUser.IsModerator())
        {
            throw new Exception("Missing permissions.");
        }
    }
}