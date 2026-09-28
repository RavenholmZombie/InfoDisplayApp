using InfoDisplayApp.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;

namespace InfoDisplayApp;

public sealed class frmInfoStore : Form
{
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly AppletManager _manager = new();
    private readonly AppletRepositoryService _repository = new();

    public event EventHandler? AppletsChanged;

    private bool _initialized;

    public frmInfoStore()
    {
        Text = "InfoStore";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(18, 18, 18);
        Controls.Add(_webView);
        Shown += frmInfoStore_Shown;
    }

    private async void frmInfoStore_Shown(object? sender, EventArgs e)
    {
        if (_initialized)
        {
            await SendCatalogAsync();
            return;
        }

        try
        {
            await _webView.EnsureCoreWebView2Async();
            _webView.CoreWebView2.WebMessageReceived -= WebMessageReceived;
            _webView.CoreWebView2.WebMessageReceived += WebMessageReceived;
            _webView.NavigateToString(BuildHtml());
            _initialized = true;
        }
        catch (Exception ex)
        {
            AppMessages.Error($"InfoStore could not start: {ex.Message}");
            Close();
        }
    }

    private async void WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement root = doc.RootElement;
            string action = root.GetProperty("action").GetString() ?? "";

            if (action == "close") { Hide(); return; }
            if (action == "refresh") { await SendCatalogAsync(); return; }
            if (!root.TryGetProperty("id", out JsonElement idElement)) return;

            string id = idElement.GetString() ?? "";
            IReadOnlyList<AppletDefinition> available = await _repository.GetAvailableAppletsAsync();
            AppletDefinition? applet = available.FirstOrDefault(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (action == "install" && applet != null)
            {
                await _manager.InstallAsync(applet);
                AppletsChanged?.Invoke(this, EventArgs.Empty);
                await SendCatalogAsync();
            }
            else if (action == "uninstall")
            {
                _manager.Uninstall(id);
                AppletsChanged?.Invoke(this, EventArgs.Empty);
                await SendCatalogAsync();
            }
        }
        catch (Exception ex)
        {
            Post(new { type = "error", message = ex.Message });
        }
    }

    private async Task SendCatalogAsync()
    {
        try
        {
            IReadOnlyList<AppletDefinition> available = await _repository.GetAvailableAppletsAsync();
            HashSet<string> installed = _manager.GetInstalledApplets().Select(a => a.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Post(new { type = "catalog", apps = available.Select(a => new {
                a.Id, a.Name, a.Version, a.Description, a.IconUrl, a.Author, installed = installed.Contains(a.Id)
            })});
        }
        catch (Exception ex)
        {
            Post(new { type = "error", message = "Unable to load the InfoStore repository: " + ex.Message });
        }
    }

    private void Post(object value) =>
        _webView.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(value));

    private static string BuildHtml() => """
<!doctype html><html><head><meta charset="utf-8"><style>
body{font-family:Segoe UI,Arial;background:#121212;color:#fff;margin:0}header{display:flex;align-items:center;padding:18px 24px;background:#202020;position:sticky;top:0}
h1{margin:0;flex:1;font-size:26px}.top{background:#444;color:#fff;border:0;border-radius:6px;padding:10px 16px;margin-left:8px;cursor:pointer}
#status{padding:18px 24px;color:#bbb}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:16px;padding:0 24px 24px}
.card{background:#242424;border-radius:10px;padding:18px;display:grid;grid-template-columns:64px 1fr;gap:14px}.icon{width:64px;height:64px;border-radius:10px;object-fit:contain;background:#333}
.name{font-size:20px;font-weight:600}.meta{color:#aaa;font-size:12px;margin-top:2px}.desc{color:#ddd;margin:10px 0;grid-column:1/3}
.action{grid-column:1/3;border:0;border-radius:6px;padding:10px;cursor:pointer;font-weight:600}.install{background:#3b82f6;color:#fff}.uninstall{background:#5a3030;color:#fff}
</style></head><body><header><h1>InfoStore</h1><button class="top" onclick="refresh()">Refresh</button><button class="top" onclick="send('close')">Close</button></header>
<div id="status">Loading applets...</div><div id="apps" class="grid"></div><script>
const send=(action,id)=>chrome.webview.postMessage(id?{action,id}:{action});const refresh=()=>{document.getElementById('status').textContent='Loading applets...';send('refresh')};
chrome.webview.addEventListener('message',e=>{const m=e.data;if(m.type==='error'){document.getElementById('status').textContent=m.message;return}if(m.type!=='catalog')return;
const box=document.getElementById('apps');box.innerHTML='';document.getElementById('status').textContent=m.apps.length?(m.apps.length+' applet(s) available'):'No applets are currently published.';
for(const a of m.apps){const c=document.createElement('div');c.className='card';
c.innerHTML='<img class="icon" src="'+(a.IconUrl||'')+'" onerror="this.style.visibility=\'hidden\'"><div><div class="name">'+a.Name+'</div><div class="meta">v'+a.Version+' - '+(a.Author||'Unknown author')+'</div></div><div class="desc">'+(a.Description||'')+'</div><button class="action '+(a.installed?'uninstall':'install')+'">'+(a.installed?'Uninstall':'Install')+'</button>';
c.querySelector('button').onclick=()=>send(a.installed?'uninstall':'install',a.Id);box.appendChild(c);}});
refresh();</script></body></html>
""";
}
