const $ = (selector) => document.querySelector(selector);
const steps = [...document.querySelectorAll(".pipeline li")];
const apiBase = new URL("./api/eidos", window.location.href).pathname.replace(/\/$/, "");
let currentRun = null;
const feedbackStorageKey = "eidos-feedback-drafts-v1";

function setConnection(ok, label) {
  const el = $("#connection");
  el.textContent = label;
  el.className = `connection ${ok ? "ok" : "bad"}`;
}

function text(selector, value) {
  $(selector).textContent = value ?? "—";
}

function resetSteps() {
  steps.forEach((step) => step.classList.remove("active", "done", "failed"));
}

function setBusy(busy) {
  $("#run-clean").disabled = busy;
  $("#run-fault").disabled = busy;
  $("#fault").disabled = busy;
  $("#run-badge").className = `badge ${busy ? "running" : "neutral"}`;
  $("#run-badge").textContent = busy ? "läuft" : "bereit";
  if (busy) {
    resetSteps();
    steps[0].classList.add("active");
    text("#run-status", "Mission läuft …");
    text("#run-message", "Signal wird typisiert, Mission disponiert und Candidate geprüft.");
  }
}

function showReasons(reasons) {
  const box = $("#reasons");
  const list = $("#reason-list");
  list.replaceChildren();
  if (!reasons?.length) {
    box.hidden = true;
    return;
  }
  for (const reason of reasons) {
    const li = document.createElement("li");
    li.textContent = reason;
    list.append(li);
  }
  box.hidden = false;
}

function renderRun(run) {
  currentRun = run;
  const promoted = run?.Promotion?.Status === "Promoted";
  const verified = run?.Metrics?.ReplayVerified === true;
  resetSteps();
  steps.forEach((step) => step.classList.add("done"));
  if (!promoted) steps[4].classList.replace("done", "failed");
  if (!verified) steps[5].classList.replace("done", "failed");

  text("#run-status", promoted ? "In ZT2 befördert" : "Fail closed: abgelehnt");
  text("#run-message", promoted
    ? "Alle risikoadaptiven Obligations sind erfüllt. Nur die lokale Sandbox wurde materialisiert."
    : "Der Candidate wurde nicht befördert. Baseline und Zielsystem blieben unverändert.");
  text("#run-id", run.RunId);
  text("#candidate-id", run.Candidate?.Id);
  text("#evidence-id", run.EvidencePack?.Id);
  text("#replay-state", verified ? "verifiziert" : "nicht verifiziert");

  const badge = $("#run-badge");
  badge.className = `badge ${promoted ? "success" : "rejected"}`;
  badge.textContent = promoted ? "promoted" : "rejected";
  showReasons(run.Promotion?.Reasons);

  $("#replay").disabled = false;
  const sandbox = $("#open-sandbox");
  sandbox.hidden = !promoted;
  sandbox.href = `${apiBase}/runs/${encodeURIComponent(run.RunId)}/sandbox`;
}

async function requestJson(url, options) {
  const response = await fetch(url, options);
  if (!response.ok) {
    const detail = await response.text();
    throw new Error(`${response.status} ${detail}`.trim());
  }
  return response.json();
}

async function run(fault) {
  setBusy(true);
  try {
    const result = await requestJson(`${apiBase}/runs`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ Fault: fault }),
    });
    renderRun(result);
    await loadHistory();
    setConnection(true, "Kernel bereit");
  } catch (error) {
    resetSteps();
    steps[0].classList.add("failed");
    text("#run-status", "Lauf fehlgeschlagen");
    text("#run-message", error.message);
    $("#run-badge").className = "badge rejected";
    $("#run-badge").textContent = "Fehler";
    setConnection(false, "Kernel nicht erreichbar");
  } finally {
    $("#run-clean").disabled = false;
    $("#run-fault").disabled = false;
    $("#fault").disabled = false;
  }
}

async function replay() {
  if (!currentRun) return;
  $("#replay").disabled = true;
  $("#replay").textContent = "prüfe …";
  try {
    const result = await requestJson(
      `${apiBase}/runs/${encodeURIComponent(currentRun.RunId)}/replay`,
      { method: "POST" },
    );
    text("#replay-state", result.Verified ? "erneut verifiziert" : "Replay fehlgeschlagen");
    steps[5].className = result.Verified ? "done" : "failed";
    if (!result.Verified) showReasons(result.Reasons);
  } catch (error) {
    text("#replay-state", error.message);
    steps[5].className = "failed";
  } finally {
    $("#replay").disabled = false;
    $("#replay").textContent = "↻ Erneut verifizieren";
  }
}

async function loadBenchmark() {
  try {
    const report = await requestJson(`${apiBase}/benchmark`);
    text("#eidos-score", `${report.Eidos.Correct}/${report.Eidos.Total}`);
    text("#base-score", `${report.LinearBaseline.Correct}/${report.LinearBaseline.Total}`);
    text("#unsafe-score", `${report.LinearBaseline.UnsafeApprovals}`);
    text("#benchmark-note", report.ScopeNote);
  } catch (error) {
    text("#benchmark-note", `Nicht verfügbar: ${error.message}`);
  }
}

function historyRow(item) {
  const row = document.createElement("div");
  row.className = "history-row";
  const state = document.createElement("span");
  const promoted = item.status === "RunPromoted";
  state.className = `history-state ${promoted ? "" : "rejected"}`;
  state.textContent = promoted ? "PROMOTED" : "REJECTED";
  const code = document.createElement("code");
  code.textContent = item.runId;
  const time = document.createElement("time");
  time.dateTime = item.startedAt;
  time.textContent = new Date(item.startedAt).toLocaleString("de-DE");
  const button = document.createElement("button");
  button.className = "quiet";
  button.textContent = "anzeigen";
  button.addEventListener("click", async () => {
    try {
      const run = await requestJson(`${apiBase}/runs/${encodeURIComponent(item.runId)}`);
      renderRun(run);
      $("#run-panel").scrollIntoView({ behavior: "smooth", block: "start" });
    } catch (error) {
      setConnection(false, error.message);
    }
  });
  row.append(state, code, time, button);
  return row;
}

async function loadHistory() {
  const host = $("#history");
  try {
    const result = await requestJson(`${apiBase}/runs`);
    host.replaceChildren();
    if (!result.runs.length) {
      const empty = document.createElement("p");
      empty.className = "muted";
      empty.textContent = "Noch keine Läufe.";
      host.append(empty);
      return;
    }
    result.runs.forEach((item) => host.append(historyRow(item)));
  } catch (error) {
    host.textContent = `Historie nicht verfügbar: ${error.message}`;
  }
}

function feedbackMarkdown() {
  const kind = $("#feedback-kind").value;
  const summary = $("#feedback-summary").value.trim();
  const details = $("#feedback-details").value.trim();
  const context = $("#feedback-context").value.trim();
  const label = kind === "bug" ? "bug" : "enhancement";
  const heading = kind === "bug" ? "Bug Report" : "Feature Request";
  const runId = currentRun?.RunId ?? "nicht angegeben";
  return [
    `# ${heading}: ${summary}`,
    "",
    `- Label: \`${label}\``,
    "- Quelle: EIDOS Studio v0.8 alpha",
    `- Run: \`${runId}\``,
    `- Erfasst: ${new Date().toISOString()}`,
    "",
    "## Beobachtung / gewünschtes Ergebnis",
    "",
    details,
    "",
    "## Reproduktion / Kontext",
    "",
    context || "Nicht angegeben.",
    "",
    "## Akzeptanzkriterium",
    "",
    "- [ ] Das beschriebene Verhalten ist reproduzierbar geprüft und dokumentiert.",
    "",
    "> Lokaler Entwurf: vor dem Einstellen auf sensible Daten und interne Infrastruktur prüfen.",
    "",
  ].join("\n");
}

function feedbackSlug() {
  return $("#feedback-summary").value.trim().toLowerCase()
    .normalize("NFKD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "")
    .slice(0, 48) || "feedback";
}

function saveFeedbackDraft(markdown) {
  let drafts = [];
  try {
    drafts = JSON.parse(localStorage.getItem(feedbackStorageKey) || "[]");
    if (!Array.isArray(drafts)) drafts = [];
  } catch {
    drafts = [];
  }
  drafts.unshift({ createdAt: new Date().toISOString(), markdown });
  localStorage.setItem(feedbackStorageKey, JSON.stringify(drafts.slice(0, 20)));
}

function validFeedback() {
  if (!$("#feedback-form").reportValidity()) return false;
  $("#feedback-state").textContent = "";
  return true;
}

function downloadFeedback() {
  if (!validFeedback()) return;
  const markdown = feedbackMarkdown();
  saveFeedbackDraft(markdown);
  const url = URL.createObjectURL(new Blob([markdown], { type: "text/markdown;charset=utf-8" }));
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = `${$("#feedback-kind").value}-${feedbackSlug()}.md`;
  anchor.click();
  URL.revokeObjectURL(url);
  $("#feedback-state").textContent = "Issue-Vorlage erstellt und lokal vorgemerkt.";
}

async function copyFeedback() {
  if (!validFeedback()) return;
  const markdown = feedbackMarkdown();
  try {
    await navigator.clipboard.writeText(markdown);
    saveFeedbackDraft(markdown);
    $("#feedback-state").textContent = "Issue-Vorlage kopiert und lokal vorgemerkt.";
  } catch {
    $("#feedback-state").textContent = "Kopieren nicht erlaubt; bitte die Download-Funktion verwenden.";
  }
}

$("#run-clean").addEventListener("click", () => run("none"));
$("#run-fault").addEventListener("click", () => run($("#fault").value));
$("#replay").addEventListener("click", replay);
$("#refresh-history").addEventListener("click", loadHistory);
$("#open-feedback").addEventListener("click", () => $("#feedback-dialog").showModal());
$("#close-feedback").addEventListener("click", () => $("#feedback-dialog").close());
$("#copy-feedback").addEventListener("click", copyFeedback);
$("#feedback-form").addEventListener("submit", (event) => {
  event.preventDefault();
  downloadFeedback();
});

Promise.all([loadBenchmark(), loadHistory()])
  .then(() => setConnection(true, "Kernel bereit"))
  .catch(() => setConnection(false, "Kernel nicht erreichbar"));

if ("serviceWorker" in navigator) {
  navigator.serviceWorker.register("./service-worker.js", { scope: "./" }).catch(() => {});
}
