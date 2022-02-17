using System.Threading.Tasks;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Rest;
using Serilog;

namespace Unix.Services.Extensions;

public class PagedTagView : PagedView
{
    public PagedTagView(PageProvider pageProvider, LocalMessage templateMessage = null) : base(pageProvider, templateMessage)
    {
    }

    protected override async ValueTask OnStopButtonAsync(ButtonEventArgs e)
    {
        await e.Interaction.Message.DeleteAsync();
    }
}