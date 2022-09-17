using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.Gateway;
using Disqord.Rest;
using Unix.Common.Attributes;

namespace Unix.Services.GatewayEventHandlers.CoreEventHandlers;

public class InteractionReceivedHandler : UnixService
{
    public InteractionReceivedHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override async ValueTask OnInteractionReceived(InteractionReceivedEventArgs e)
    {
        if (e.GetType().Assembly.CustomAttributes.OfType<DoNotDefer>().FirstOrDefault() is not null)
        {
            await e.Interaction.Response().DeferAsync();
        }
    }
}