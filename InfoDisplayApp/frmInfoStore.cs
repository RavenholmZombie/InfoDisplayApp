using InfoDisplayApp.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Text.Json;

namespace InfoDisplayApp;

public sealed class frmInfoStore : Form
{
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly AppletManager _manager = new();
    private readonly AppletRepositoryService _repository = new();
    private readonly Dictionary<string, int> _updateFailures = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? AppletsChanged;
    public event EventHandler? CloseRequested;

    private bool _initialized;
    private bool _openUpdatesWhenReady;

    public frmInfoStore()
    {
        Text = "InfoStore";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(18, 18, 18);
        Controls.Add(_webView);
        Shown += frmInfoStore_Shown;
    }

    public async void ShowUpdatesCenter()
    {
        _openUpdatesWhenReady = true;

        if (_initialized && _webView.CoreWebView2 != null)
        {
            await SendCatalogAsync();
            Post(new { type = "showUpdates" });
            _openUpdatesWhenReady = false;
        }
    }

    private async void frmInfoStore_Shown(object? sender, EventArgs e)
    {
        if (_initialized)
        {
            await SendCatalogAsync();
            if (_openUpdatesWhenReady)
            {
                Post(new { type = "showUpdates" });
                _openUpdatesWhenReady = false;
            }
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

            if (action == "close") { CloseRequested?.Invoke(this, EventArgs.Empty); return; }
            if (action == "refresh") { await SendCatalogAsync(); return; }
            if (action == "checkUpdates")
            {
                Post(new { type = "updateCheckState", state = "checking" });
                await SendCatalogAsync();
                Post(new { type = "updateCheckState", state = "complete" });
                return;
            }

            if (action == "updateAll")
            {
                IReadOnlyList<AppletUpdate> updates = await _repository.GetUpdatesAsync(_manager);
                foreach (AppletUpdate update in updates)
                    await UpdateAppletAsync(update.Available);
                Post(new { type = "updateAllComplete" });
                return;
            }

            if (!root.TryGetProperty("id", out JsonElement idElement)) return;
            string id = idElement.GetString() ?? "";

            IReadOnlyList<AppletDefinition> available = await _repository.GetAvailableAppletsAsync();
            AppletDefinition? applet = available.FirstOrDefault(a =>
                a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

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
            else if (action == "update" && applet != null)
            {
                await UpdateAppletAsync(applet);
            }
        }
        catch (Exception ex)
        {
            LogUpdateFailure("InfoStore update-center operation failed.", ex);
            Post(new { type = "error", message = ex.Message });
        }
    }

    private async Task UpdateAppletAsync(AppletDefinition applet)
    {
        Post(new { type = "updateState", id = applet.Id, state = "updating" });

        try
        {
            await _manager.UpdateAsync(applet);
            _updateFailures.Remove(applet.Id);
            AppletsChanged?.Invoke(this, EventArgs.Empty);
            Post(new { type = "updateState", id = applet.Id, state = "current" });
        }
        catch (Exception ex)
        {
            int failures = _updateFailures.TryGetValue(applet.Id, out int count) ? count + 1 : 1;
            _updateFailures[applet.Id] = failures;

            LogUpdateFailure(
                $"Applet update failed for {applet.Id} ({applet.Version}), attempt {failures}.", ex);
            Post(new { type = "updateState", id = applet.Id, state = "failed" });

            if (failures >= 2)
            {
                AppMessages.Error(
                    $"InfoScreen was unable to update {applet.Name} after multiple attempts. " +
                    $"The failure has been written to the InfoScreen logs.\r\n\r\n{ex.Message}");
            }
        }
    }

    private async Task SendCatalogAsync()
    {
        try
        {
            IReadOnlyList<AppletDefinition> available = await _repository.GetAvailableAppletsAsync();
            IReadOnlyList<AppletDefinition> installedList = _manager.GetInstalledApplets();
            Dictionary<string, AppletDefinition> installed = installedList
                .ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);

            Post(new
            {
                type = "catalog",
                apps = available.Select(a =>
                {
                    installed.TryGetValue(a.Id, out AppletDefinition? local);
                    bool updateAvailable = local != null &&
                        AppletRepositoryService.CompareVersions(a.Version, local.Version) > 0;

                    return new
                    {
                        a.Id,
                        a.Name,
                        a.Version,
                        a.Description,
                        a.IconUrl,
                        a.Author,
                        installed = local != null,
                        installedVersion = local?.Version,
                        updateAvailable
                    };
                })
            });

            if (_openUpdatesWhenReady)
            {
                Post(new { type = "showUpdates" });
                _openUpdatesWhenReady = false;
            }
        }
        catch (Exception ex)
        {
            Post(new { type = "error", message = "Unable to load the InfoStore repository: " + ex.Message });
        }
    }

    private static void LogUpdateFailure(string message, Exception ex)
    {
        try
        {
            string directory = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"InfoScreen-APPLET-UPDATE-{DateTime.Now:yyyy-MM-dd}.log");
            File.AppendAllText(path,
                $"{DateTime.Now:O} {message}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception logEx)
        {
            Debug.WriteLine($"Unable to write applet update log: {logEx}");
        }
    }

    private void Post(object value) =>
        _webView.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(value));

    private static string BuildHtml() => """
<!doctype html><html><head><meta charset="utf-8"><style>
:root{font-size:20px}*{box-sizing:border-box}
body{font-family:Segoe UI,Arial;background:#121212;color:#fff;margin:0;overflow-x:hidden}
header{display:flex;align-items:center;padding:22px 30px;background:#202020;position:sticky;top:0;z-index:5;min-height:92px}
.brand{display:flex;align-items:center;gap:16px;flex:1}.store-icon{width:58px;height:58px;object-fit:contain}
h1{margin:0;font-size:34px;line-height:1}.top{background:#444;color:#fff;border:0;border-radius:8px;padding:14px 22px;margin-left:12px;cursor:pointer;font-size:18px;font-weight:600;min-width:110px}
#status{padding:24px 30px 18px;color:#bbb;font-size:20px}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(390px,1fr));gap:22px;padding:0 30px 30px;max-width:1500px}
.card{background:#242424;border-radius:14px;padding:22px;display:grid;grid-template-columns:88px 1fr;gap:18px;min-height:260px}.icon{width:88px;height:88px;border-radius:14px;object-fit:contain;background:#333}
.name{font-size:27px;font-weight:600;line-height:1.15;margin-top:3px}.meta{color:#aaa;font-size:16px;margin-top:7px}.desc{color:#ddd;margin:14px 0 4px;grid-column:1/3;font-size:20px;line-height:1.35}
.action{grid-column:1/3;border:0;border-radius:8px;padding:14px;cursor:pointer;font-weight:700;font-size:19px;min-height:52px}.install,.update{background:#3b82f6;color:#fff}.uninstall{background:#6b3434;color:#fff}
.current{background:#3f4a42;color:#bbb;cursor:default}.failed{background:#b42318;color:#fff}.updating{background:#555;color:#ddd;cursor:wait}
#updatesPage{display:none}.updates-head{display:flex;align-items:center;padding:24px 30px 18px;gap:16px}.updates-head h2{margin:0;flex:1;font-size:30px}.version-arrow{color:#ddd;font-size:18px;margin-top:8px}
@media (min-width:1600px){:root{font-size:22px}.grid{grid-template-columns:repeat(auto-fill,minmax(440px,1fr));max-width:1700px}.card{min-height:285px}}
</style></head><body>
<header><div class="brand"><img class="store-icon" src="https://raw.githubusercontent.com/RavenholmZombie/InfoScreenAppRepository/main/AppIcons/infostore.png" onerror="this.style.display='none'"><h1>InfoStore</h1></div><button class="top" onclick="showUpdates()">Updates</button><button class="top" onclick="refresh()">Refresh</button><button class="top" onclick="send('close')">Close</button></header>
<div id="catalogPage"><div id="status">Loading applets...</div><div id="apps" class="grid"></div></div>
<div id="updatesPage"><div class="updates-head"><h2>Applet Updates Center</h2><button id="checkUpdates" class="top" onclick="checkUpdates()">Check for Updates</button><button id="updateAll" class="top" onclick="updateAll()">Update All</button><button class="top" onclick="showCatalog()">Back</button></div><div id="updateStatus"></div><div id="updates" class="grid"></div></div>
<script>
let apps=[];
const send=(action,id)=>chrome.webview.postMessage(id?{action,id}:{action});
const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const refresh=()=>{document.getElementById('status').textContent='Loading applets...';send('refresh')};
const showCatalog=()=>{document.getElementById('catalogPage').style.display='block';document.getElementById('updatesPage').style.display='none'};
const showUpdates=()=>{document.getElementById('catalogPage').style.display='none';document.getElementById('updatesPage').style.display='block';renderUpdates()};
const checkUpdates=()=>{
 const b=document.getElementById('checkUpdates');
 b.disabled=true;b.textContent='Checking...';
 document.getElementById('updateStatus').innerHTML='<div id="status">Checking the InfoScreen App Repository for updates...</div>';
 send('checkUpdates');
};
const updateAll=()=>{document.getElementById('updateAll').disabled=true;send('updateAll')};

function renderCatalog(){
 const box=document.getElementById('apps');box.innerHTML='';
 document.getElementById('status').textContent=apps.length?(apps.length+' applet(s) available'):'No applets are currently published.';
 for(const a of apps){const c=document.createElement('div');c.className='card';
 c.innerHTML='<img class="icon" src="'+esc(a.IconUrl||'')+'" onerror="this.style.visibility=\'hidden\'"><div><div class="name">'+esc(a.Name)+'</div><div class="meta">v'+esc(a.Version)+' - '+esc(a.Author||'Unknown author')+'</div></div><div class="desc">'+esc(a.Description||'')+'</div><button class="action '+(a.installed?'uninstall':'install')+'">'+(a.installed?'Uninstall':'Install')+'</button>';
 c.querySelector('button').onclick=()=>send(a.installed?'uninstall':'install',a.Id);box.appendChild(c);}
}
function renderUpdates(){
 const box=document.getElementById('updates');box.innerHTML='';
 const pending=apps.filter(a=>a.installed&&a.updateAvailable);
 document.getElementById('updateStatus').innerHTML='<div id="status">'+(pending.length?(pending.length+' applet update(s) available'):'All installed applets are up-to-date.')+'</div>';
 document.getElementById('updateAll').disabled=pending.length===0;
 for(const a of pending){const c=document.createElement('div');c.className='card';c.dataset.id=a.Id;
 c.innerHTML='<img class="icon" src="'+esc(a.IconUrl||'')+'" onerror="this.style.visibility=\'hidden\'"><div><div class="name">'+esc(a.Name)+'</div><div class="version-arrow">Installed: v'+esc(a.installedVersion)+' &nbsp;→&nbsp; Available: v'+esc(a.Version)+'</div></div><div class="desc">'+esc(a.Description||'')+'</div><button class="action update">Update</button>';
 c.querySelector('button').onclick=()=>{setState(a.Id,'updating');send('update',a.Id)};box.appendChild(c);}
}
function setState(id,state){
 const card=document.querySelector('#updates .card[data-id="'+CSS.escape(id)+'"]');if(!card)return;
 const b=card.querySelector('button');b.className='action '+state;
 if(state==='updating'){b.textContent='Updating...';b.disabled=true}
 if(state==='current'){b.textContent='Up-to-date';b.disabled=true}
 if(state==='failed'){b.textContent='Failed. Retry?';b.disabled=false;b.onclick=()=>{setState(id,'updating');send('update',id)}}
}
chrome.webview.addEventListener('message',e=>{const m=e.data;
 if(m.type==='error'){document.getElementById('status').textContent=m.message;return}
 if(m.type==='showUpdates'){showUpdates();return}
 if(m.type==='updateCheckState'){
   const b=document.getElementById('checkUpdates');
   if(m.state==='checking'){b.disabled=true;b.textContent='Checking...'}
   if(m.state==='complete'){b.disabled=false;b.textContent='Check for Updates'}
   return
 }
 if(m.type==='updateState'){
   setState(m.id,m.state);
   if(m.state==='current'){const a=apps.find(x=>x.Id===m.id);if(a){a.installedVersion=a.Version;a.updateAvailable=false}}
   return
 }
 if(m.type==='updateAllComplete'){document.getElementById('updateAll').disabled=true;return}
 if(m.type!=='catalog')return;
 apps=m.apps;renderCatalog();if(document.getElementById('updatesPage').style.display==='block')renderUpdates();
});
refresh();
</script></body></html>
""";
}
