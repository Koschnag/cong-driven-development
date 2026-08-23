const stages = {
  source: {
    number: "Stage 01",
    status: "Preserved",
    title: "The record starts before the summary.",
    description: "Original bytes, provenance and digest remain addressable. A generated summary may help retrieval, but never replaces its source.",
    input: "Owner-approved files, sessions and typed records",
    output: "A stable source identity and immutable digest",
    boundary: "No silent mutation, no invented provenance"
  },
  claim: {
    number: "Stage 02",
    status: "Typed",
    title: "Every statement declares what kind of statement it is.",
    description: "Observation, proposal, inference and ratified fact do not collapse into one confident paragraph. Status travels with the claim.",
    input: "Source-bound observations or explicitly marked proposals",
    output: "A versioned claim with scope, status and evidence links",
    boundary: "No unsupported certainty, no orphaned assertion"
  },
  candidate: {
    number: "Stage 03",
    status: "Labelled lab",
    title: "Experiments stay experiments.",
    description: "EIDOS may explore plans, interpretations and changes inside a bounded lane. Its output is a candidate—not a shortcut into Core memory.",
    input: "Mission, constraints, typed claims and a read-only twin",
    output: "A comparable candidate with obligations and risks",
    boundary: "The lab cannot ratify itself or write Core truth"
  },
  assurance: {
    number: "Stage 04",
    status: "Independent",
    title: "The generator is not its own reviewer.",
    description: "Tests, policy checks, provenance validation and representative restore evidence are produced or evaluated across explicit boundaries.",
    input: "Candidate, acceptance criteria and frozen evidence rules",
    output: "Evidence pack with failures and missing obligations intact",
    boundary: "No green label from narrative confidence"
  },
  promotion: {
    number: "Stage 05",
    status: "Human gate",
    title: "Authority is a decision, not a side effect.",
    description: "Promotion binds the exact candidate, exact evidence and exact owner decision. Rejection and deferral remain first-class outcomes.",
    input: "Digest-bound candidate and complete evidence pack",
    output: "Approved, rejected or deferred promotion receipt",
    boundary: "No model, n8n flow or agent may self-promote"
  },
  outcome: {
    number: "Stage 06",
    status: "Replayable",
    title: "The change leaves a receipt.",
    description: "A typed action records identity, scope, idempotency and result. The system can explain what happened without exposing private payloads.",
    input: "Approved intent through the existing action adapter",
    output: "Bounded effect, owner-visible result and replay evidence",
    boundary: "Pause and revoke remain available"
  }
};

const buttons = [...document.querySelectorAll("[data-stage]")];
const fields = {
  number: document.querySelector("#stage-number"),
  status: document.querySelector("#stage-status"),
  title: document.querySelector("#stage-title"),
  description: document.querySelector("#stage-description"),
  input: document.querySelector("#stage-input"),
  output: document.querySelector("#stage-output"),
  boundary: document.querySelector("#stage-boundary")
};

const selectStage = (button) => {
  const stage = stages[button.dataset.stage];
  if (!stage) return;

  buttons.forEach((candidate) => {
    candidate.setAttribute("aria-selected", String(candidate === button));
    candidate.tabIndex = candidate === button ? 0 : -1;
  });

  Object.entries(fields).forEach(([key, node]) => {
    if (node) node.textContent = stage[key];
  });
};

buttons.forEach((button, index) => {
  button.addEventListener("click", () => selectStage(button));
  button.addEventListener("keydown", (event) => {
    if (!["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) return;
    event.preventDefault();
    let next = index;
    if (event.key === "ArrowDown") next = (index + 1) % buttons.length;
    if (event.key === "ArrowUp") next = (index - 1 + buttons.length) % buttons.length;
    if (event.key === "Home") next = 0;
    if (event.key === "End") next = buttons.length - 1;
    buttons[next].focus();
    selectStage(buttons[next]);
  });
});

const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
if (!reducedMotion && "IntersectionObserver" in window) {
  document.documentElement.classList.add("reveal-ready");
  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        entry.target.classList.add("in-view");
        observer.unobserve(entry.target);
      }
    });
  }, {threshold: 0.12});

  document.querySelectorAll(".section, .finale").forEach((section) => observer.observe(section));
}
