using Microsoft.JSInterop;

namespace Ebt.App.Infrastructure;

/// <summary>The browser window, for what Blazor can't do without JavaScript.</summary>
internal sealed class BrowserWindow(IJSRuntime js)
{
    public ValueTask ScrollToTop() => js.InvokeVoidAsync("scrollTo", 0, 0);
}
