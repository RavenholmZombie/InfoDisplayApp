using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace InfoDisplayApp;

public sealed class ctrlAppletWebView : UserControl
{
    private readonly WebView2 _webView;
    private bool _muted;

    public ctrlAppletWebView()
    {
        // Keep the host background consistent with a normal browser. A black default
        // background bleeds through any page areas whose CSS background is transparent.
        _webView = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.White };
        Controls.Add(_webView);
        _webView.CoreWebView2InitializationCompleted += WebView_Initialized;
    }

    public async Task NavigateAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Applet URL must be an absolute HTTP or HTTPS URL.", nameof(url));

        await _webView.EnsureCoreWebView2Async();
        _webView.Source = uri;
        _webView.CoreWebView2.IsMuted = _muted;
    }

    public void SetMuted(bool muted)
    {
        _muted = muted;
        if (_webView.CoreWebView2 != null) _webView.CoreWebView2.IsMuted = muted;
    }

    private void WebView_Initialized(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess && _webView.CoreWebView2 != null)
        {
            _webView.CoreWebView2.IsMuted = _muted;

            // Applets are arbitrary websites, so don't force InfoScreen's dark host
            // appearance onto them. Light matches normal browser rendering and keeps
            // transparent/unstyled page backgrounds from becoming black.
            _webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Light;
        }
    }
}
