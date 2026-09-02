const $ = (selector, root = document) => root.querySelector(selector);
const api = (path) => new URL(path, document.baseURI).toString();
const state = { workspaces: [], portfolio: { contracts: [], assurance: [] }, selected: 0 };

const escapeHtml = (value = "") => String(value).replace(/[&<>'"]/g, (char) => ({
  "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;",
})[char]);
const label = (value = "unknown") => String(value).replace(/([a-z])([A-Z])/g, "$1 $2");
const stateClass = (value = "") => String(value).toLowerCase();
const shortCommit = (value = "") => value ? value.slice(0, 12) : "—";
const date = (value) => {
  if (!value) return "—";
  const parsed = new Date(value);
  return Number.isNaN(parsed.valueOf()) ? value : new Intl.DateTimeFormat("de-DE", { dateStyle: "medium", timeStyle: "short" }).format(parsed);
};

async function readJson(path) {
  const response = await fetch(api(path), { headers: { Accept: "application/json" } });
  if (!response.ok) throw new Error(response.status === 403
    ? "Diese Projektion ist deaktiviert. Starte CDD Web mit CDD_ENABLE_WORKSPACES=true."
    : `API ${response.status}`);
  return response.json();
}

function renderMetrics() {
  const workspaces = state.workspaces;
  $("#metric-workspaces").textContent = workspaces.length;
  $("#metric-runs").textContent = workspaces.reduce((sum, item) => sum + (item.AgenticRuns?.Total || 0), 0);
  $("#metric-evidence").textContent = workspaces.reduce((sum, item) => sum + (item.AgenticRuns?.FullSolves || 0), 0);
  $("#metric-contracts").textContent = state.portfolio.contracts.length;
}

function renderWorkspaceList() {
  const root = $("#workspace-list");
  root.innerHTML = state.workspaces.map((workspace, index) => `
    <button class="workspace-button ${index === state.selected ? "active" : ""}" data-index="${index}" type="button">
      <header><b>${escapeHtml(workspace.Name)}</b><i class="state-dot ${stateClass(workspace.State)}"></i></header>
      <small>${escapeHtml(workspace.Git.Branch || "kein Git")} · ${workspace.WorkItems.Ready + workspace.WorkItems.Running} offen</small>
    </button>`).join("");
  root.querySelectorAll("button").forEach((button) => button.addEventListener("click", () => {
    state.selected = Number(button.dataset.index);
    renderWorkspaceList();
    renderWorkspaceDetail();
  }));
}

const count = (value, title) => `<div class="count"><b>${value}</b><span>${title}</span></div>`;
const agenticPhases = ["Scouting", "Building", "Gating", "Critiquing", "Repairing", "Reviewing", "Checkpointing", "SliceComplete"];

function agenticRunCard(run) {
  if (!run) return `<section class="detail-card autopilot-chain"><h3>Full-Agentic SDLC Chain</h3>
    <p class="panel-intro">Noch kein persistenter Autopilot-Run. Ein Run entsteht mit <code>cdd autopilot init</code>.</p></section>`;
  const metrics = run.Evaluation || {};
  const phases = agenticPhases.map((phase) => `<div class="phase ${phase === run.Phase ? "active" : ""} ${phase === "SliceComplete" && metrics.FullSolve ? "done" : ""}">
    <i></i><span>${escapeHtml(phase)}</span></div>`).join("");
  const worker = run.CurrentRole
    ? `${escapeHtml(run.CurrentRole)} · ${escapeHtml(run.Provider || "provider")} / ${escapeHtml(run.Model || "model")} · ${escapeHtml(run.Harness || "harness")}`
    : "deterministischer Controller-Schritt";
  return `<section class="detail-card autopilot-chain">
    <div class="agentic-head"><div><h3>Full-Agentic SDLC Chain</h3><b>${escapeHtml(run.MissionId)} · ${escapeHtml(run.ActiveSliceId)}</b>
      <p>${escapeHtml(run.ActiveSliceTitle)} · ${escapeHtml(run.LifecycleStage)}</p></div>
      <span class="state-badge ${stateClass(run.Status)}">${escapeHtml(run.Status)}</span></div>
    <div class="phase-chain">${phases}</div>
    <div class="agentic-now"><span>Nächste Aktion</span><b>${escapeHtml(run.NextAction)}</b><small>${worker}</small></div>
    <div class="agentic-metrics">
      ${count(metrics.CompletedSlices || 0, `Slices / ${metrics.TotalSlices || 0}`)}
      ${count(metrics.AgentTurns || 0, "Agent Turns")}
      ${count(metrics.GateRuns || 0, "Gate Runs")}
      ${count(metrics.PrematureStops || 0, "Premature Stops")}
      ${count(metrics.RepairCycles || 0, "Repairs")}
      ${count(metrics.HumanInterventions || 0, "Human Touches")}
    </div>
    ${run.BlockReasons?.length ? `<ul class="reason-list">${run.BlockReasons.map((reason) => `<li>${escapeHtml(reason)}</li>`).join("")}</ul>` : ""}
  </section>`;
}

function renderWorkspaceDetail() {
  const workspace = state.workspaces[state.selected];
  const root = $("#workspace-detail");
  if (!workspace) {
    root.innerHTML = '<div class="empty-state"><span>∅</span><p>Kein Workspace verbunden.</p></div>';
    return;
  }
  const mission = workspace.ActiveMission;
  const run = workspace.LatestRun;
  const agenticRun = workspace.ActiveAgenticRun;
  const remote = workspace.Git.Remote && /^https?:\/\//.test(workspace.Git.Remote)
    ? `<a href="${escapeHtml(workspace.Git.Remote)}" target="_blank" rel="noreferrer">Repository öffnen ↗</a>` : "—";
  root.innerHTML = `
    <header class="detail-head">
      <div><p>${escapeHtml(workspace.Id)} · ${escapeHtml(workspace.Git.Branch || "unversioniert")}</p><h2>${escapeHtml(workspace.Name)}</h2>
        ${workspace.StateReasons.length ? `<ul class="reason-list">${workspace.StateReasons.map((reason) => `<li>${escapeHtml(reason)}</li>`).join("")}</ul>` : ""}
      </div>
      <span class="state-badge ${stateClass(workspace.State)}">${escapeHtml(workspace.State)}</span>
    </header>
    <div class="detail-grid">
      ${agenticRunCard(agenticRun)}
      <section class="detail-card mission"><h3>Active Mission</h3>
        ${mission ? `<div class="mission-id">${escapeHtml(mission.Id)} · ${escapeHtml(mission.Status)}</div><h4>${escapeHtml(mission.Title)}</h4><p>${escapeHtml(mission.Objective)}</p><div class="gates">${mission.RequiredGates.map((gate) => `<span>${escapeHtml(gate)}</span>`).join("")}</div>`
          : '<p class="panel-intro">Keine aktive Mission im Adapterformat gefunden.</p>'}
      </section>
      <section class="detail-card repo-card"><h3>Repository Actual State</h3><div class="repo-lines">
        <div class="repo-line"><span>Commit</span><b>${escapeHtml(shortCommit(workspace.Git.Commit))} · ${escapeHtml(workspace.Git.CommitTitle || "—")}</b></div>
        <div class="repo-line"><span>Synchronisation</span><b>↑ ${workspace.Git.Ahead} · ↓ ${workspace.Git.Behind} · ${workspace.Git.DirtyFiles} lokal</b></div>
        <div class="repo-line"><span>Remote</span>${remote}</div>
      </div></section>
      <section class="detail-card lifecycle"><h3>Work Item Lifecycle</h3><div class="count-strip">
        ${count(workspace.WorkItems.Draft, "Draft")}${count(workspace.WorkItems.Ready, "Ready")}${count(workspace.WorkItems.Running, "Running")}${count(workspace.WorkItems.Review, "Review")}${count(workspace.WorkItems.Accepted, "Accepted")}${count(workspace.WorkItems.Blocked, "Blocked")}
      </div></section>
      <section class="detail-card runs-card"><h3>Evidence & Runs</h3><div class="run-summary">
        ${count(workspace.Runs.Total, "Total")}${count(workspace.Runs.Running, "Running")}${count(workspace.Runs.Succeeded, "Success")}${count(workspace.Runs.WithSummary, "Summary")}
      </div>${run ? `<div class="latest-run"><b>${escapeHtml(run.Id)}</b><br>${escapeHtml(run.Status)} · ${date(run.StartedAt)} · ${run.HasSummary ? "Evidence summary" : "noch ohne Summary"}</div>` : ""}
        <div class="source-row">${workspace.Sources.map((source) => `<code>${escapeHtml(source)}</code>`).join("")}</div>
      </section>
    </div>`;
  $("#observed-at").textContent = `Beobachtet ${date(workspace.ObservedAt)}`;
}

function renderPortfolio() {
  const { contracts, assurance } = state.portfolio;
  $("#assurance-count").textContent = `${assurance.length} Verfahren`;
  $("#contract-count").textContent = `${contracts.length} Verträge`;
  $("#contract-chips").innerHTML = contracts.map((item) => `<span>${escapeHtml(item.Standard)}</span>`).join("");
  $("#assurance-names").textContent = assurance.slice(0, 6).map((item) => item.Name).join(" · ");
  $("#assurance-list").innerHTML = assurance.map((item) => `
    <div class="assurance-item"><b>${escapeHtml(item.Name)}</b><span>${escapeHtml(label(item.Technique))}</span><p>${escapeHtml(item.Purpose)}</p></div>`).join("");
  $("#contract-list").innerHTML = contracts.map((item) => `
    <div class="contract-item"><b>${escapeHtml(item.Name)}</b><span>${escapeHtml(item.Direction)} · ${escapeHtml(item.Standard)}</span><p>${escapeHtml(item.Purpose)}</p></div>`).join("");
}

async function refresh() {
  const button = $("#refresh");
  button.disabled = true;
  try {
    const [workspaceData, portfolio] = await Promise.all([
      readJson("api/studio/workspaces"), readJson("api/studio/portfolio"),
    ]);
    state.workspaces = workspaceData.workspaces || [];
    state.portfolio = portfolio;
    if (state.selected >= state.workspaces.length) state.selected = 0;
    renderMetrics(); renderWorkspaceList(); renderWorkspaceDetail(); renderPortfolio();
  } catch (error) {
    $("#workspace-list").innerHTML = `<div class="error-box">${escapeHtml(error.message)}</div>`;
    $("#workspace-detail").innerHTML = `<div class="empty-state"><span>!</span><p>${escapeHtml(error.message)}</p></div>`;
  } finally { button.disabled = false; }
}

$("#refresh").addEventListener("click", refresh);
refresh();
