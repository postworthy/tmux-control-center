using System.Net;
using System.Text.Json;

namespace TmuxCtl.Desktop;

public static class ProfileChooserPage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Render(IReadOnlyList<ServerProfile> profiles, string? error = null)
    {
        var profileJson = JsonSerializer.Serialize(profiles, JsonOptions);
        var errorMarkup = string.IsNullOrWhiteSpace(error)
            ? ""
            : $"<div class=\"error\" role=\"alert\">{WebUtility.HtmlEncode(error)}</div>";
        return $$$"""
          <!doctype html>
          <html lang="en"><head><meta charset="utf-8"><meta name="color-scheme" content="dark">
          <meta name="viewport" content="width=device-width,initial-scale=1"><title>tmuxctl servers</title>
          <style>
          :root{font-family:Inter,Ubuntu,system-ui,sans-serif;color:#e6e8eb;background:#111418}
          *{box-sizing:border-box}body{margin:0;min-height:100vh;background:#111418}button,input{font:inherit}
          .shell{width:min(760px,calc(100vw - 48px));margin:46px auto}.brand{color:#72d6a4;font-size:22px;font-weight:750}
          h1{margin:8px 0 5px;font-size:28px}p{margin:0;color:#8f99a5}.profiles{display:grid;gap:8px;margin:28px 0}
          .profile{display:grid;grid-template-columns:1fr auto auto;align-items:center;gap:8px;padding:13px 14px;background:#191d22;border:1px solid #30363d;border-radius:7px}
          .profile strong,.profile small{display:block}.profile small{margin-top:3px;color:#8f99a5}.profile button,.form button{padding:8px 12px;color:#e6e8eb;background:#262d34;border:1px solid #3c4650;border-radius:5px;cursor:pointer}
          .profile .connect,.form .save{background:#287a58;border-color:#33966d}.profile .delete:hover{color:#ff8c85;border-color:#8c4642}
          .empty{padding:20px;color:#8f99a5;background:#171b20;border:1px dashed #39414a;border-radius:7px}
          .form{display:grid;grid-template-columns:180px 1fr auto;gap:8px;padding-top:20px;border-top:1px solid #30363d}
          input{min-width:0;padding:9px 10px;color:#eef1f4;background:#0f1216;border:1px solid #39414a;border-radius:5px;outline:none}input:focus{border-color:#72d6a4;box-shadow:0 0 0 1px #72d6a4}
          .form h2{grid-column:1/-1;margin:0 0 3px;font-size:16px}.error{margin:14px 0;padding:10px 12px;color:#ffd8d5;background:#5a2424;border:1px solid #a44;border-radius:6px}
          @media(max-width:680px){.form{grid-template-columns:1fr}.form h2{grid-column:auto}}
          </style></head><body><main class="shell"><div class="brand">tmuxctl</div><h1>Servers</h1>
          <p>Choose an existing tmuxctl server. Tailscale and the server must already be running.</p>
          {{{errorMarkup}}}<section id="profiles" class="profiles"></section>
          <form id="profile-form" class="form"><h2 id="form-title">Add server</h2>
          <input id="profile-id" type="hidden"><input id="label" maxlength="80" required placeholder="Label">
          <input id="url" required inputmode="url" placeholder="https://tmux.example.ts.net">
          <button class="save" type="submit">Save and connect</button></form></main>
          <script>
          const profiles={{{profileJson}}};
          const send=value=>window.external.sendMessage(JSON.stringify(value));
          const host=document.getElementById('profiles');
          if(!profiles.length)host.innerHTML='<div class="empty">No saved servers yet.</div>';
          for(const profile of profiles){
            const row=document.createElement('div');row.className='profile';
            const text=document.createElement('div');const strong=document.createElement('strong');strong.textContent=profile.label;
            const small=document.createElement('small');small.textContent=profile.serverUrl;text.append(strong,small);
            const connect=document.createElement('button');connect.className='connect';connect.textContent='Connect';connect.onclick=()=>send({type:'connect',id:profile.id});
            const menu=document.createElement('button');menu.textContent='Edit';menu.onclick=()=>{document.getElementById('profile-id').value=profile.id;document.getElementById('label').value=profile.label;document.getElementById('url').value=profile.serverUrl;document.getElementById('form-title').textContent='Edit server';document.getElementById('label').focus()};
            const remove=document.createElement('button');remove.className='delete';remove.textContent='Delete';remove.onclick=()=>{if(confirm(`Delete server profile “${profile.label}”?`))send({type:'delete',id:profile.id})};
            const actions=document.createElement('span');actions.append(menu,remove);row.append(text,connect,actions);host.append(row);
          }
          document.getElementById('profile-form').onsubmit=event=>{event.preventDefault();send({type:'saveAndConnect',id:document.getElementById('profile-id').value||null,label:document.getElementById('label').value,url:document.getElementById('url').value})};
          </script></body></html>
          """;
    }

    public static string RenderConnecting(string host) => $$$"""
      <!doctype html>
      <html lang="en"><head><meta charset="utf-8"><meta name="color-scheme" content="dark">
      <meta name="viewport" content="width=device-width,initial-scale=1"><title>Connecting to tmuxctl</title>
      <style>
      :root{font-family:Inter,Ubuntu,system-ui,sans-serif;color:#e6e8eb;background:#111418}
      body{display:grid;place-items:center;min-height:100vh;margin:0;background:#111418}
      main{text-align:center}.brand{color:#72d6a4;font-size:22px;font-weight:750}
      h1{margin:10px 0 6px;font-size:25px}p{margin:0;color:#8f99a5}
      </style></head><body><main><div class="brand">tmuxctl</div><h1>Checking server compatibility</h1>
      <p>{{{WebUtility.HtmlEncode(host)}}}</p></main></body></html>
      """;
}
