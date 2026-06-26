/* Statische IDE-Demo: fängt /api/*-Aufrufe ab und liefert echte, gesnapshottete
 * Antworten des Self-Modells (66 Knoten) aus. Lesen = echt; Schreiben/Live-Agent
 * = schreibgeschützt (kein Backend, kein Login). Läuft als klassisches Script
 * VOR den ES-Modulen, patcht window.fetch, bevor die App ihn benutzt. */
(function () {
  var SNAP = { '/api/spot': 'spot.json', '/api/validate': 'validate.json', '/api/diff': 'diff.json', '/api/history': 'history.json' };
  var orig = window.fetch.bind(window);
  function j(o) { return new Response(JSON.stringify(o), { status: 200, headers: { 'content-type': 'application/json' } }); }
  var shown = false;
  function badge() {
    if (shown || !document.body) return; shown = true;
    var d = document.createElement('div');
    d.innerHTML = '🔒 <b>Demo-Modus</b> — echtes Self-Modell (66 Knoten), aber <b>schreibgeschützt</b>: Live-Agent &amp; Speichern sind hier aus. Voll nutzbar via <a href="https://codespaces.new/Koschnag/cong-driven-development" target="_blank" style="color:#4ea1ff">Codespaces</a>.';
    d.style.cssText = 'position:fixed;bottom:12px;left:50%;transform:translateX(-50%);max-width:92vw;background:#161b22;color:#e6edf3;border:1px solid #d29922;border-radius:8px;padding:8px 16px;font:13px/1.45 system-ui,-apple-system,Segoe UI,Roboto,sans-serif;z-index:99999;box-shadow:0 6px 20px rgba(0,0,0,.45)';
    document.body.appendChild(d);
  }
  window.fetch = function (input, init) {
    var url = (typeof input === 'string') ? input : (input && input.url) || '';
    var u = url.replace(/^https?:\/\/[^/]+/, '');
    var method = ((init && init.method) || 'GET').toUpperCase();
    if (u.indexOf('/api/') !== 0) return orig(input, init);          // Assets etc. → echt
    for (var k in SNAP) { if (u === k || u.indexOf(k + '?') === 0) return orig('_demo/' + SNAP[k]); }
    if (u.indexOf('/api/export') === 0) return orig('_demo/export.md');
    if (u.indexOf('/api/history/') === 0) return j([]);             // Knoten-Historie leer
    if (u.indexOf('/api/providers') === 0) return j([]);            // keine LLM-Provider in der Demo
    if (u.indexOf('/api/dwh') === 0) return j([]);                  // @-Gedächtnis leer
    if (u.indexOf('/api/infra') === 0) return j({ services: [] });
    // Schreiben / Streaming / Agent → schreibgeschützt
    setTimeout(badge, 200);
    if (u.indexOf('/api/engine') === 0 || u.indexOf('/api/loop') === 0 || u.indexOf('/api/schmiede') === 0)
      return new Response('', { status: 200, headers: { 'content-type': 'text/event-stream' } });
    if (method === 'GET') return j({});
    return j({ ok: true, demo: true });
  };
  if (document.readyState !== 'loading') setTimeout(badge, 400);
  else document.addEventListener('DOMContentLoaded', function () { setTimeout(badge, 400); });
})();
