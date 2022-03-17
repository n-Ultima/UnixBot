using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Unix.Modules.Bases;
using Unix.Services.Core.Abstractions;

namespace Unix.Modules;

public class ConfigurationModule : UnixAdminSlashCommandModuleBase
{
    public ConfigurationModule(IGuildService guildService, ITagService tagService, IModerationService moderationService, IPhishermanService phishermanService, IOwnerService ownerService, IReminderService reminderService, IReactionRoleService reactionRoleService) 
        : base(guildService, tagService, moderationService, phishermanService, ownerService, reminderService, reactionRoleService)
    {
    }
    
    [SlashCommand("configure-adminrole", "Configures the administrator role for your guild.")]
    public async Task ConfigAdminRoleAsync([Summary("role", "The role to set as the adminrole.")] IRole role)
    {
        await GuildService.ModifyGuildAdminRoleAsync(Context.Guild.Id, role.Id);
        await SendSuccessAsync("Successfully configured adminrole.");
    }

    [SlashCommand("configure-modrole", "Configures the moderator role for your guild.")]
    public async Task ConfigModRoleAsync([Summary("role", "The role to set as the modrole.")] IRole role)
    {
        await GuildService.ModifyGuildModRoleAsync(Context.Guild.Id, role.Id);
        await SendSuccessAsync("Successfully configured modrole.");
    }
}