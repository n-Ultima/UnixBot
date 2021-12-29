using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Unix.Data;
using Unix.Data.Models.Core;
using Unix.Services.Core.Abstractions;

namespace Unix.Services.Core;

public class TagService : UnixService, ITagService
{
    public TagService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <inheritdoc /> 
    public async Task CreateTagAsync(Snowflake guildId, Snowflake ownerId, string tagName, string tagContent)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var tag = await unixContext.Tags
                .Where(x => x.Name == tagName)
                .Where(x => x.GuildId == guildId)
                .SingleOrDefaultAsync();
            if (tag != null)
            {
                throw new Exception("Tag with that name already exists.");
            }

            unixContext.Tags.Add(new Tag
            {
                GuildId = guildId,
                OwnerId = ownerId,
                Name = tagName,
                Content = tagContent,
            });
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc /> 
    public async Task<IEnumerable<Tag>> FetchTagsAsync(Snowflake guildId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.Tags
                .Where(x => x.GuildId == guildId)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }

    public async Task<Tag> FetchTagAsync(Snowflake guildId, string tagName)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            return await unixContext.Tags
                .Where(x => x.GuildId == guildId)
                .Where(x => x.Name == tagName)
                .SingleOrDefaultAsync();
        }
    }

    /// <inheritdoc /> 
    public async Task EditTagContentAsync(Snowflake guildId, Snowflake requestorId, string tagName, string tagContent)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            var tag = await unixContext.Tags
                .Where(x => x.GuildId == guildId)
                .Where(x => x.Name == tagName)
                .SingleOrDefaultAsync();
            if (tag == null)
            {
                throw new Exception("Tag with that name does not exist.");
            }

            var guildUserRequestor = await Bot.FetchMemberAsync(guildId, requestorId);
            if (!CanUserMaintainTag(guildConfig, tag, guildUserRequestor))
            {
                throw new Exception("You must either have moderator/administrator permissions or own the tag to edit it.");
            }

            tag.Content = tagContent;
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc /> 
    public async Task EditTagOwnershipAsync(Snowflake guildId, Snowflake requestorId, string tagName, Snowflake newOwnerId)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            var tag = await unixContext.Tags
                .Where(x => x.GuildId == guildId)
                .Where(x => x.Name == tagName)
                .SingleOrDefaultAsync();
            if (tag == null)
            {
                throw new Exception("Tag with that name does not exist.");
            }

            var guildUserRequestor = await Bot.FetchMemberAsync(guildId, requestorId);
            if (!CanUserMaintainTag(guildConfig, tag, guildUserRequestor))
            {
                throw new Exception("You must either have moderator/administrator permissions or own the tag to transfer ownership of it.");
            }

            tag.OwnerId = newOwnerId;
            await unixContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc /> 
    public async Task DeleteTagAsync(Snowflake guildId, Snowflake requestorId, string tagName)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var unixContext = scope.ServiceProvider.GetRequiredService<UnixContext>();
            var guildConfig = await unixContext.GuildConfigurations
                .FindAsync(guildId);
            var tag = await unixContext.Tags
                .Where(x => x.GuildId == guildId)
                .Where(x => x.Name == tagName)
                .SingleOrDefaultAsync();
            if (tag == null)
            {
                throw new Exception("Tag with that name does not exist.");
            }

            var guildUserRequestor = await Bot.FetchMemberAsync(guildId, requestorId);
            if (!CanUserMaintainTag(guildConfig, tag, guildUserRequestor))
            {
                throw new Exception("You must either have the moderator/administrator permissions or own the tag to delete it.");
            }

            unixContext.Tags.Remove(tag);
            await unixContext.SaveChangesAsync();
        }
    }

    private bool CanUserMaintainTag(GuildConfiguration guildConfig, Tag tag, IMember member)
    {
        if (tag.OwnerId == member.Id)
        {
            return true;
        }

        if (member.GetGuild().OwnerId == member.Id)
        {
            return true;
        }
        if (member.RoleIds.Any())
        {
            if (member.RoleIds.Contains(guildConfig.ModeratorRoleId) || member.RoleIds.Contains(guildConfig.AdministratorRoleId))
            {
                return true;
            }
        }

        return false;
    }
}