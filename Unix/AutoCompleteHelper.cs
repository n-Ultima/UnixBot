using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Unix.Data.Models.Moderation;
using Unix.Modules;
using Unix.Services;

namespace Unix;

public class AutoCompleteHelper : UnixService
{
    public AutoCompleteHelper(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override ValueTask OnInteractionReceived(InteractionReceivedEventArgs e)
    {
        if (e.Interaction.Type is not InteractionType.ApplicationCommandAutoComplete)
        {
            return ValueTask.CompletedTask;
            
        }

        // just a friendly cast here.
        if (e.Interaction is not IAutoCompleteInteraction autoCompleteInteraction)
        {
            return ValueTask.CompletedTask;
        }

        if (!autoCompleteInteraction.GuildId.HasValue)
        {
            return ValueTask.CompletedTask;
        }
        
        InfractionModule._autoCompleteGuildId = autoCompleteInteraction.GuildId.Value;

        if (autoCompleteInteraction.Author is not IMember member)
        {
            return ValueTask.CompletedTask;
        }

        InfractionModule._autoCompleteMember = member;
        return ValueTask.CompletedTask;
    }
}