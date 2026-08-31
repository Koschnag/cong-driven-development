(() => {
  "use strict";
  const SPOT_URL = "../ide/_demo/spot.json";
  const REPO = "https://github.com/Koschnag/cong-driven-development";
  const byId = (id) => document.getElementById(id);
  const item = (entry) => entry?.Payload?.Fields?.Item || {};
  const kind = (entry) => entry?.Payload?.Case || "Unknown";
  const el = (tag, className, text) => {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
  };
  const append = (parent, ...children) =>
    children.forEach((child) => child && parent.append(child));
  const feedbackButton = (reference) => {
    const button = el("button", "feedback-link", "Feedback →");
    button.type = "button";
    button.dataset.feedbackRef = reference;
    return button;
  };

  function renderClaims(entries) {
    const claims = entries.filter((e) => kind(e) === "ResearchClaimNode");
    const root = byId("claim-list");
    claims.forEach((entry, index) => {
      const data = item(entry);
      const card = el("article", "claim");
      const number = el(
        "div",
        "claim-index",
        String(index + 1).padStart(2, "0"),
      );
      const main = el("div", "claim-main");
      append(
        main,
        el("h3", "", data.Statement || entry.Id),
        el("p", "", data.Rationale || "Keine Begründung hinterlegt."),
      );
      const meta = el("div", "claim-meta");
      append(
        meta,
        el("span", "tag", data.Status || entry.Convergence),
        el("span", "", data.Scope || "Scope offen"),
        el("span", "", data.Provenance?.Method || "Methode offen"),
        feedbackButton(entry.Id),
      );
      append(card, number, main, meta);
      root.append(card);
    });
    return claims.length;
  }

  function renderSources(entries) {
    const sources = entries.filter((e) => kind(e) === "KnowledgeNode");
    const root = byId("source-list");
    sources.forEach((entry) => {
      const data = item(entry);
      const link = el("a", "source");
      link.href = data.Source || `${REPO}/blob/main/.spot/${entry.Id}.json`;
      link.target = "_blank";
      link.rel = "noopener";
      append(
        link,
        el("span", "", data.MediaType || "Quelle"),
        el("b", "", data.Title || entry.Id),
        el("small", "", "Quelle öffnen ↗"),
      );
      root.append(link);
    });
    return sources.length;
  }

  function renderRisks(entries) {
    const risks = entries.filter((e) => kind(e) === "RiskNode");
    const root = byId("risk-list");
    risks.slice(0, 8).forEach((entry) => {
      const data = item(entry);
      const card = el(
        "article",
        `risk ${data.Impact === "Critical" ? "critical" : ""}`,
      );
      const meta = el("div", "risk-meta");
      append(
        meta,
        el("span", "", `IMPACT ${data.Impact || "?"}`),
        el("span", "", `LIKELIHOOD ${data.Likelihood || "?"}`),
      );
      append(
        card,
        meta,
        el("h3", "", data.Statement || entry.Id),
        el("p", "", data.Mitigation || "Mitigation offen"),
        feedbackButton(entry.Id),
      );
      root.append(card);
    });
    const pending = entries
      .filter((e) => e.Convergence !== "Aligned")
      .slice(0, 16);
    pending.forEach((entry) => {
      const row = el("div", "pending");
      append(
        row,
        el("code", "", entry.Id),
        document.createTextNode(` · ${kind(entry).replace("Node", "")}`),
      );
      byId("pending-list").append(row);
    });
    return pending.length;
  }

  function renderPremises(entries) {
    entries
      .filter((e) => kind(e) === "PremiseNode")
      .forEach((entry) => {
        const data = item(entry);
        const card = el("article", "mini-card");
        append(
          card,
          el("h3", "", data.Statement || entry.Id),
          el("p", "", data.Rationale || "Rationale offen"),
          feedbackButton(entry.Id),
        );
        byId("premise-list").append(card);
      });
  }

  const projectCatalog = [
    [
      "Cdd.Core",
      "Getypter SPOT-Kernel, Invarianten, Evidence und Runtime-Grenzen",
      "src/Cdd.Core",
    ],
    [
      "Cdd.Cli",
      "Deterministische Modell- und Validierungsoperationen",
      "src/Cdd.Cli",
    ],
    [
      "Cdd.Mcp",
      "Werkzeugschnittstelle für kontrollierte Agenten",
      "src/Cdd.Mcp",
    ],
    [
      "Cdd.Web",
      "Interaktives CDD Studio und read-only Projektionen",
      "src/Cdd.Web",
    ],
    [
      "EIDOS",
      "Evidence-gated Intent-to-Outcome Development System",
      "src/Cdd.Core/Eidos.fs",
    ],
    [
      "CourseForge",
      "Generisches Lernspiel-Referenzprojekt aus Moodle-Metadaten",
      "examples/CourseForge.Core",
    ],
    [
      "Research Track",
      "Paper, Claims, Methodik und Benchmark-Protokolle",
      "research",
    ],
    [
      "Research Studio",
      "Dieses briefing-orientierte Review-Interface",
      "docs/research",
    ],
  ];
  function renderProjects(entries) {
    const components = new Map(
      entries
        .filter((e) => kind(e) === "ComponentNode")
        .map((e) => [item(e).Name, e.Convergence]),
    );
    projectCatalog.forEach(([name, description, path]) => {
      const card = el("article", "project");
      const top = el("div", "project-top");
      append(
        top,
        el("h3", "", name),
        el("code", "", components.get(name) || "versioniert"),
      );
      const link = el("a", "", "Quellen öffnen →");
      link.href = `${REPO}/tree/main/${path}`;
      append(card, top, el("p", "", description), link);
      byId("project-list").append(card);
    });
  }

  function renderRoadmap(entries) {
    const decisions = entries.filter((e) => kind(e) === "DecisionNode");
    decisions.forEach((entry) => {
      const data = item(entry);
      const row = el("article");
      append(
        row,
        el("code", "", entry.Id),
        el("h3", "", data.Title || entry.Id),
        el("p", "", `${data.Choice || ""} ${data.Consequences || ""}`.trim()),
        feedbackButton(entry.Id),
      );
      byId("decision-list").append(row);
    });
  }

  function installFeedback() {
    const dialog = byId("feedback-dialog");
    document.addEventListener("click", (event) => {
      const trigger = event.target.closest("[data-feedback-ref]");
      if (!trigger) return;
      byId("feedback-ref").value = trigger.dataset.feedbackRef;
      dialog.showModal();
    });
    byId("feedback-form").addEventListener("submit", (event) => {
      if (event.submitter?.value === "cancel") return;
      event.preventDefault();
      const ref = byId("feedback-ref").value;
      const kindValue = byId("feedback-kind").value;
      const summary = byId("feedback-summary").value.trim();
      if (!summary) {
        byId("feedback-summary").focus();
        return;
      }
      const details = byId("feedback-details").value.trim();
      const title = `[Research Feedback] ${summary}`;
      const body = `## Bezug\n${ref}\n\n## Art\n${kindValue}\n\n## Feedback\n${details || summary}\n\n## Akzeptanzfrage\nWelche Evidenz würde dieses Feedback bestätigen oder widerlegen?\n\n— erstellt im öffentlichen Research Studio; vor dem Absenden geprüft`;
      window.open(
        `${REPO}/issues/new?title=${encodeURIComponent(title)}&body=${encodeURIComponent(body)}&labels=research-feedback`,
        "_blank",
        "noopener",
      );
    });
  }

  async function boot() {
    installFeedback();
    try {
      const response = await fetch(SPOT_URL, { cache: "no-store" });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const entries = await response.json();
      const claims = renderClaims(entries);
      const sources = renderSources(entries);
      const pending = renderRisks(entries);
      renderPremises(entries);
      renderProjects(entries);
      renderRoadmap(entries);
      // No public export is available yet; avoid presenting derived counts as
      // longitudinal production evidence.
      byId("metric-nodes").textContent = "unknown";
      byId("metric-claims").textContent = "unknown";
      byId("metric-sources").textContent = "unknown";
      byId("metric-open").textContent = "unknown";
      byId("snapshot-label").textContent =
        "SPOT-Snapshot · Exportstatus unknown · read-only";
    } catch (error) {
      byId("snapshot-label").textContent = "SPOT-Snapshot nicht verfügbar";
      const note = el(
        "p",
        "error",
        "Der versionierte SPOT-Snapshot konnte nicht geladen werden. Bitte die Seite über einen Webserver statt direkt als Datei öffnen.",
      );
      byId("claim-list").append(note);
    }
  }
  boot();
})();
