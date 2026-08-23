# CDD im Homelab betreiben — wie ein Cloud-Programm

Das Cockpit „Cong OS" läuft als Container und speichert den SPOT-Graphen in einem
Volume. Es bringt **keine eigene Authentifizierung** mit — exponiere es deshalb nie
direkt, sondern immer hinter einem Reverse-Proxy mit Auth.

Zwei Wege.

## Weg A — Selbstständig (compose + Caddy + Basic-Auth)

Turnkey, ein Befehl. Caddy terminiert TLS und erzwingt Basic-Auth.

```bash
git clone https://github.com/Koschnag/cong-driven-development
cd cong-driven-development

# 1) Passwort-Hash erzeugen
docker run --rm caddy:2 caddy hash-password --plaintext 'DEIN_PASS'

# 2) Konfig anlegen
cp .env.example .env
#   CDD_PASS_HASH=<hash aus Schritt 1> eintragen — ROH, ohne Anführungszeichen,
#   das '$' NICHT verdoppeln (env_file reicht den Wert literal durch).
#   optional: CDD_DOMAIN=cdd.example.com  (echte Domain → Let's-Encrypt)

# 3) Starten
docker compose up -d
```

> **Hash-Falle:** Der bcrypt-Hash enthält `$`. Würde er über `${..}`-Interpolation
> laufen, zerlegt Docker Compose ihn — deshalb kommt er hier über `env_file` (literal).
> In der `.env` daher **keine Quotes** und **kein `$$`-Escaping**.

Erreichbar unter `https://<CDD_DOMAIN>` (Standard `localhost`, internes TLS-Zertifikat →
Browser-Warnung im Dev-Fall). Bei einer echten, auf den Host zeigenden Domain holt Caddy
automatisch ein Let's-Encrypt-Zertifikat (Port 80 + 443 müssen öffentlich erreichbar sein).
Lokal ohne root: in der `.env` `CDD_HTTP_PORT`/`CDD_HTTPS_PORT` auf Werte > 1024 setzen.

Update: `docker compose pull && docker compose up -d`. Der SPOT bleibt im Volume `cdd-data`.

## Weg B — Hinter deinem bestehenden DC-Proxy

Wenn schon YunoHost/SSOwat, Coolify oder ein Caddy/Traefik läuft: nur den nackten
Container fahren und davorhängen.

```bash
docker run -d --name cdd --restart unless-stopped \
  -p 127.0.0.1:8080:8080 \
  -v /srv/cdd/data:/data \
  ghcr.io/koschnag/cdd:latest
```

Dann im vorhandenen Proxy eine Subdomain auf `127.0.0.1:8080` zeigen lassen und die
Auth des Proxys (SSO/OIDC/LDAP) davorlegen. Eine öffentliche Instanz darf nie
ohne vorgeschaltete Authentifizierung betrieben werden.

## Die Chat-Loop (optional)

CLI, MCP-Server und Konvergenz-Orakel laufen ohne weitere Konfiguration. Nur die
**chat-primäre Engine-Kette** des Cockpits braucht einen Modell-Zugang — per
`ANTHROPIC_API_KEY` und/oder einem erreichbaren `OLLAMA_HOST` (in `.env`).

Schreibende und betriebsnahe Funktionen sind zusätzlich **default-deny**:

- `CDD_ALLOW_MUTATIONS=true` aktiviert SPOT-Schreiben, EIDOS-Runs, Provider,
  Engine und Konvergenz-Loop;
- `CDD_ENABLE_MEMORY=true` aktiviert den optionalen, sanitisierten Knowledge Store;
- `CDD_ENABLE_INFRA=true` legt lokale Host-, Metrik- und Container-Metadaten offen.
- `CDD_ENABLE_WORKSPACES=true` aktiviert read-only Git-/SPOT-/`.ai`-Projektionen
  für explizit per `--workspace` verbundene Repositories; Hostpfade bleiben verborgen.

Diese Flags nur in einer authentifizierten, privaten Operator-Instanz setzen. Die
öffentliche Forschungsdarstellung unter `docs/` ist statisch und benötigt keinen
dieser Zugriffe.

## Architektur-Hinweise

- **Daten:** ein JSON-File pro SPOT-Knoten unter `/data` — git-freundlich, sicherbar.
- **Backup:** das Volume `cdd-data` (bzw. `/srv/cdd/data`) in die DC-Backup-Pipeline aufnehmen.
- **Arch:** das veröffentlichte Image ist `linux/amd64`. Für ARM-Hosts das Image
  lokal bauen: `docker build -t cdd-arm .`.
