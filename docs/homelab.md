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
#   CDD_PASS_HASH=<hash aus Schritt 1> eintragen
#   optional: CDD_DOMAIN=cdd.cong42.de  (echte Domain → Let's-Encrypt)

# 3) Starten
docker compose up -d
```

Erreichbar unter `https://<CDD_DOMAIN>:8443` (Standard `localhost`). Bei einer echten,
auf den Host zeigenden Domain holt Caddy automatisch ein Let's-Encrypt-Zertifikat —
dann Port `443` mappen statt `8443`.

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
Auth des Proxys (SSO/LDAP) davorlegen. Empfohlen für die `cong42.de`-Linie.

## Die Chat-Loop (optional)

CLI, MCP-Server und Konvergenz-Orakel laufen ohne weitere Konfiguration. Nur die
**chat-primäre Engine-Kette** des Cockpits braucht einen Modell-Zugang — per
`ANTHROPIC_API_KEY` und/oder einem erreichbaren `OLLAMA_HOST` (in `.env`).

## Architektur-Hinweise

- **Daten:** ein JSON-File pro SPOT-Knoten unter `/data` — git-freundlich, sicherbar.
- **Backup:** das Volume `cdd-data` (bzw. `/srv/cdd/data`) in die DC-Backup-Pipeline aufnehmen.
- **Arch:** das veröffentlichte Image ist `linux/amd64` — läuft auf x86-Hosts
  (Celsius/Tower). Für ARM (Pi) das Image lokal bauen: `docker build -t cdd-arm .`.
