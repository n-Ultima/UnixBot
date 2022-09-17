using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Rest;
using Serilog;

namespace Unix.Services.Extensions;

public class PagedTagView : PagedView
{
    public PagedTagView(PageProvider pageProvider, Action<LocalMessageBase> messageTemplate = null) : base(pageProvider, messageTemplate)
    {
    }

    // This just makes sure we get rid of the embed that generates when /tag-list is ran.
    protected override async ValueTask OnStopButton(ButtonEventArgs e)
    {
        await e.Interaction.Message.DeleteAsync();
    }
}