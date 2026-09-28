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
:root{font-size:20px}
*{box-sizing:border-box}
body{font-family:Segoe UI,Arial;background:#121212;color:#fff;margin:0;overflow-x:hidden}
header{display:flex;align-items:center;padding:22px 30px;background:#202020;position:sticky;top:0;z-index:5;min-height:92px}
.brand{display:flex;align-items:center;gap:16px;flex:1}
.store-icon{width:58px;height:58px;object-fit:contain}
h1{margin:0;font-size:34px;line-height:1}
.top{background:#444;color:#fff;border:0;border-radius:8px;padding:14px 22px;margin-left:12px;cursor:pointer;font-size:18px;font-weight:600;min-width:110px}
#status{padding:24px 30px 18px;color:#bbb;font-size:20px}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(390px,1fr));gap:22px;padding:0 30px 30px;max-width:1500px}
.card{background:#242424;border-radius:14px;padding:22px;display:grid;grid-template-columns:88px 1fr;gap:18px;min-height:260px}
.icon{width:88px;height:88px;border-radius:14px;object-fit:contain;background:#333}
.name{font-size:27px;font-weight:600;line-height:1.15;margin-top:3px}
.meta{color:#aaa;font-size:16px;margin-top:7px}
.desc{color:#ddd;margin:14px 0 4px;grid-column:1/3;font-size:20px;line-height:1.35}
.action{grid-column:1/3;border:0;border-radius:8px;padding:14px;cursor:pointer;font-weight:700;font-size:19px;min-height:52px}
.install{background:#3b82f6;color:#fff}.uninstall{background:#6b3434;color:#fff}
@media (min-width:1600px){:root{font-size:22px}.grid{grid-template-columns:repeat(auto-fill,minmax(440px,1fr));max-width:1700px}.card{min-height:285px}}
</style></head><body>
<header><div class="brand"><img class="store-icon" src="https://raw.githubusercontent.com/RavenholmZombie/InfoScreenAppRepository/main/AppIcons/infostore.png" onerror="this.style.display='none'"><h1>InfoStore</h1></div><button class="top" onclick="refresh()">Refresh</button><button class="top" onclick="send('close')">Close</button></header>
<div id="status">Loading applets...</div><div id="apps" class="grid"></div><script>
const send=(action,id)=>chrome.webview.postMessage(id?{action,id}:{action});const refresh=()=>{document.getElementById('status').textContent='Loading applets...';send('refresh')};
chrome.webview.addEventListener('message',e=>{const m=e.data;if(m.type==='error'){document.getElementById('status').textContent=m.message;return}if(m.type!=='catalog')return;
const box=document.getElementById('apps');box.innerHTML='';document.getElementById('status').textContent=m.apps.length?(m.apps.length+' applet(s) available'):'No applets are currently published.';
for(const a of m.apps){const c=document.createElement('div');c.className='card';
c.innerHTML='<img class="icon" src="'+(a.IconUrl||'')+'" onerror="this.style.visibility=\'hidden\'"><div><div class="name">'+a.Name+'</div><div class="meta">v'+a.Version+' - '+(a.Author||'Unknown author')+'</div></div><div class="desc">'+(a.Description||'')+'</div><button class="action '+(a.installed?'uninstall':'install')+'">'+(a.installed?'Uninstall':'Install')+'</button>';
c.querySelector('button').onclick=()=>send(a.installed?'uninstall':'install',a.Id);box.appendChild(c);}});
refresh();</script></body></html>
"""
}
