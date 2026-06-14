// Agent-Interface: Prosa → Modelländerung.
// Zwei Wege: (A) Prompt für beliebige LLMs erzeugen/kopieren,
// (B) direkt die Claude Messages API aus dem Browser aufrufen.

const KEY_STORAGE = "cdd-anthropic-key";

// Antwort-Vertrag, den das LLM erfüllen muss.
const contract = `Antworte AUSSCHLIESSLICH mit einem JSON-Objekt in exakt dieser Form (kein Markdown, kein Text drumherum):
{
  "summary": "<1-2 Sätze: was wurde geändert und warum>",
  "upsert": [ <vollständige SPOT-Knoten, die angelegt oder überschrieben werden> ],
  "delete": [ "<id>", ... ]
}
Ein SPOT-Knoten hat die Form {"Id": "...", "Payload": {"Case": "<SpecNode|TestNode|RiskNode|InfraNode|ComponentNode|PremiseNode|DecisionNode|KnowledgeNode|ToolNode|TermNode>", "Fields": {"Item": {...}}}, "Convergence": "<Pending|Aligned|Diverged|Orphaned>"}.
Ids nur aus [a-zA-Z0-9_-], beginnend mit Buchstabe/Ziffer. Beziehungen (Relations/DependsOn/SpecRef/Supersedes) müssen auf existierende Knoten zeigen. Halte dich strikt an die ubiquitäre Sprache aus dem Kontext.`;

export function buildPrompt(prose, contextMd) {
  return `Du bist ein Modellierungs-Agent für CDD (cong-driven-development). Unten der aktuelle SPOT-Graph als Kontextpaket. Führe die gewünschte Änderung am Modell durch.

${contract}

=== KONTEXT (SPOT-Graph) ===
${contextMd}

=== GEWÜNSCHTE ÄNDERUNG ===
${prose}`;
}

export const getApiKey = () => localStorage.getItem(KEY_STORAGE) ?? "";
export const setApiKey = (k) =>
  k ? localStorage.setItem(KEY_STORAGE, k) : localStorage.removeItem(KEY_STORAGE);

// Robustes Herauslösen des JSON-Objekts aus der Modellantwort.
export function parseChanges(text) {
  const start = text.indexOf("{");
  const end = text.lastIndexOf("}");
  if (start < 0 || end <= start) throw new Error("Antwort enthält kein JSON-Objekt");
  const obj = JSON.parse(text.slice(start, end + 1));
  if (!Array.isArray(obj.upsert)) obj.upsert = [];
  if (!Array.isArray(obj.delete)) obj.delete = [];
  // Nur Einträge mit gültiger Id durchlassen — sonst landet ein PUT auf /api/spot/undefined.
  const gültigeId = (id) => typeof id === "string" && /^[a-zA-Z0-9][a-zA-Z0-9_-]*$/.test(id);
  obj.upsert = obj.upsert.filter((e) => e && gültigeId(e.Id));
  obj.delete = obj.delete.filter((id) => gültigeId(id));
  return obj;
}

// Direkter Browser-Aufruf der Claude Messages API.
export async function callClaude({ apiKey, model, prose, contextMd }) {
  const res = await fetch("https://api.anthropic.com/v1/messages", {
    method: "POST",
    headers: {
      "content-type": "application/json",
      "x-api-key": apiKey,
      "anthropic-version": "2023-06-01",
      "anthropic-dangerous-direct-browser-access": "true",
    },
    body: JSON.stringify({
      model,
      max_tokens: 16000,
      // Adaptives Thinking gibt es erst ab Opus/Sonnet 4.6 — Haiku 4.5 würde mit 400 ablehnen.
      ...(model.startsWith("claude-haiku") ? {} : { thinking: { type: "adaptive" } }),
      system: `Du bist ein präziser Modellierungs-Agent für SPOT-Graphen (CDD). ${contract}`,
      messages: [
        {
          role: "user",
          content: `=== KONTEXT (SPOT-Graph) ===\n${contextMd}\n\n=== GEWÜNSCHTE ÄNDERUNG ===\n${prose}`,
        },
      ],
    }),
  });
  const raw = await res.text();
  let body;
  try {
    body = JSON.parse(raw);
  } catch {
    // Kein JSON (z. B. CORS-/Gateway-HTML-Fehlerseite) → verständliche Meldung statt SyntaxError
    throw new Error(`Antwort war kein JSON (HTTP ${res.status}). Anfang: ${raw.slice(0, 120)}`);
  }
  if (!res.ok) throw new Error(body?.error?.message ?? `HTTP ${res.status}`);
  const text = body.content
    .filter((b) => b.type === "text")
    .map((b) => b.text)
    .join("");
  return parseChanges(text);
}
