using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Humanizer;
using Qmmands;
using Unix.Modules.Attributes;
using Unix.Modules.Bases;
using Unix.Services.Core;
using Color = System.Drawing.Color;

namespace Unix.Modules
{
    [Group("role")]
    public class RoleModule : UnixGuildModuleBase
    {
        private readonly GuildService _guildService;

        public RoleModule(GuildService guildService)
        {
            _guildService = guildService;
        }
        [Command("", "add")]
        [Description("Adds the provided role to the user.")]
        public async Task<DiscordCommandResult> AddRoleAsync(
            [Description("The role to add.")]
                IRole role)
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(Context.GuildId);
            if (!guildConfig.SelfAssignableRoles.Contains(role.Id.RawValue))
            {
                return Failure("Unknown role.");
            }

            await Context.Author.GrantRoleAsync(role.Id);
            return Success($"{Context.Author.Mention} granted you **{role.Name}**");
        }

        [Command("", "add")]
        [Description("Adds the provided role to the user.")]
        public async Task<DiscordCommandResult> AddRoleAsync(
            [Description("The role to add.")]
                string roleName)
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(Context.GuildId);
            var role = Context.Guild.Roles
                .Where(x => x.Value.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value)
                .FirstOrDefault();
            if (role == null || !guildConfig.SelfAssignableRoles.Contains(role.Id.RawValue))
            {
                return Failure("Unknown role.");
            }

            await Context.Author.GrantRoleAsync(role.Id);
            return Success($"{Context.Author.Mention} granted you **{role.Name}**");
        }
        
        [Command("configure-add")]
        [Description("Adds the provided role to the guilds list of assignable roles.")]
        [RequireGuildAdministrator]
        public async Task<DiscordCommandResult> AddSelfRoleAsync(
            [Description("The role to add.")] 
                IRole role)
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(Context.GuildId);
            if (guildConfig.SelfAssignableRoles.Contains(role.Id.RawValue))
            {
                return Failure("That role already exists in the database.");
            }

            await _guildService.AddSelfAssignableRoleAsync(Context.GuildId, role.Id);
            return Success($"Users can now add this role with `{guildConfig.Prefix}role {role.Name}`");
        }

        [Command("configure-remove")]
        [Description("Removes the provided role from the guilds list of assignable roles.")]
        [RequireGuildAdministrator]
        public async Task<DiscordCommandResult> RemoveSelfRoleAsync(
            [Description("The role to remove.")] 
                IRole role)
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(Context.GuildId);
            if (!guildConfig.SelfAssignableRoles.Contains(role.Id.RawValue))
            {
                return Failure("That role does not exist in the database.");
            }

            await _guildService.RemoveSelfAssignableRoleAsync(Context.GuildId, role.Id);
            return Success("Role removed");
            
        }

        [Command("remove")]
        [Description("Removes the role provided from you.")]
        public async Task<DiscordCommandResult> RemoveRoleAsync(
            [Description("The role to remove.")] IRole role)
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(Context.GuildId);
            if (!guildConfig.SelfAssignableRoles.Contains(role.Id.RawValue))
            {
                return Failure("Unknown role.");
            }

            await Context.Author.RevokeRoleAsync(role.Id);
            return Success($"{Context.Author.Mention} removed the **{role.Name}** role.");
        }

        [Command("remove")]
        [Description("Removes the role provided from you.")]
        public async Task<DiscordCommandResult> RemoveRoleAsync(
            [Description("The role to remove.")] string roleName)
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(Context.GuildId);
            var role = Context.Guild.Roles
                .Where(x => x.Value.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value)
                .FirstOrDefault();
            if (role == null || !guildConfig.SelfAssignableRoles.Contains(role.Id.RawValue))
            {
                return Failure("Unknown role.");
            }

            await Context.Author.RevokeRoleAsync(role.Id);
            return Success($"{Context.Author.Mention} removed the **{role.Name}** role.");
        }
        
        [Command("")]
        [Description("Shows a helpful embed.")]
        public async Task<DiscordCommandResult> DisplayRoleHelpAsync()
        {
            var guildConfig = await _guildService.FetchGuildConfigurationAsync(Context.GuildId);
            List<string> roles = new();
            foreach (var roleId in guildConfig.SelfAssignableRoles)
            {
                roles.Add($"<@&{roleId}>");
            }

            var embed = new LocalEmbed()
                .WithAuthor(Context.Guild.Name, Context.Guild.GetIconUrl())
                .WithTitle("How do I get roles?")
                .WithColor(Color.Aqua)
                .WithDescription(
                    $"To give yourself a role, use the `{guildConfig.Prefix}role <roleName>` where **roleName** is whatever role you want.\nTo remove a role, use the `{guildConfig.Prefix}role remove <roleName>` replacing **roleName** with the role you want to remove.\n")
                .AddField("Roles available to you:", !roles.Any()
                ? "None"
                : roles.Humanize());
            return Response(embed);
        }
    }
}