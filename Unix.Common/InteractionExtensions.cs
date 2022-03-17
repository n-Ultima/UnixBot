using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;

namespace Unix.Common;


public enum PermissionLevel
{
    Moderator,
    Administrator
}
public static class InteractionExtensions
{
    public static async Task SendEphmeralErrorAsync(this InteractionReceivedEventArgs eventArgs, PermissionLevel permissionLevel)
    {
        if (permissionLevel == PermissionLevel.Administrator)
        {
            await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent($"⚠️ User lacks the required `Administrator` permission to use this command."));
        }

        if (permissionLevel == PermissionLevel.Moderator)
        {
            await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent($"⚠️ User lacks the required `Moderator` permission to use this command."));
        }
    }

    public static async Task SendEphmeralErrorAsync(this InteractionReceivedEventArgs eventArgs, string errorMessage)
    {
        await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
            .WithIsEphemeral()
            .WithContent($"⚠ ️{errorMessage}"));
    }

    public static async Task SendSuccessAsync(this InteractionReceivedEventArgs eventArgs, string message)
    {
        await eventArgs.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
            .WithContent($"<:unixok:884524202458222662> {message}"));
    }

    public static LocalInteractionMessageResponse ToLocalInteractionResponse(this LocalMessage message)
    {
        var localInteraction = new LocalInteractionMessageResponse()
            .WithContent(message.Content)
            .WithEmbeds(message.Embeds)
            .WithComponents(message.Components);
        return localInteraction;
    }
}