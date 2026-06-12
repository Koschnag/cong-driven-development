// Formular-Editor: echte Eingabefelder pro Knotenart statt rohem JSON.
// buildForm(entry, entries, opts) → { el, getValue() }

const LEVELS = ["Low", "Medium", "High", "Critical"];
const CONVERGENCE = ["Pending", "Aligned", "Diverged", "Orphaned"];
const REL_KINDS = [
  ["IsA", "ist ein"],
  ["PartOf", "Teil von"],
  ["RelatesTo", "bezieht sich auf"],
];

const schemas = {
  SpecNode: [
    { key: "Title", label: "Titel", type: "text" },
    { key: "Intent", label: "Intent — das Was, nicht das Wie", type: "textarea" },
    { key: "Criteria", label: "Akzeptanzkriterien", type: "criteria" },
  ],
  TermNode: [
    { key: "Name", label: "Begriff", type: "text" },
    { key: "Definition", label: "Definition", type: "textarea" },
    { key: "Synonyms", label: "Synonyme (Komma-getrennt)", type: "csv" },
    { key: "Relations", label: "Beziehungen zu anderen Begriffen", type: "relations" },
  ],
  ComponentNode: [
    { key: "Name", label: "Name", type: "text" },
    { key: "DependsOn", label: "Hängt ab von", type: "refs" },
  ],
  RiskNode: [
    { key: "Statement", label: "Risiko", type: "textarea" },
    { key: "Likelihood", label: "Wahrscheinlichkeit", type: "select", options: LEVELS },
    { key: "Impact", label: "Auswirkung", type: "select", options: LEVELS },
    { key: "Mitigation", label: "Mitigation (leer = keine)", type: "nulltext" },
  ],
  PremiseNode: [
    { key: "Statement", label: "Prämisse", type: "text" },
    { key: "Rationale", label: "Begründung", type: "textarea" },
  ],
  DecisionNode: [
    { key: "Title", label: "Titel", type: "text" },
    { key: "Context", label: "Kontext", type: "textarea" },
    { key: "Choice", label: "Entscheidung", type: "textarea" },
    { key: "Consequences", label: "Konsequenzen", type: "textarea" },
    { key: "Supersedes", label: "Ersetzt Entscheidung (optional)", type: "refnull" },
  ],
  KnowledgeNode: [
    { key: "Title", label: "Titel", type: "text" },
    { key: "Source", label: "Quelle (URL, Pfad oder ISBN)", type: "text" },
    { key: "MediaType", label: "Art", type: "select", options: ["book", "pdf", "link", "blog"] },
    { key: "Takeaways", label: "Takeaways (eine pro Zeile)", type: "lines" },
  ],
  ToolNode: [
    { key: "Name", label: "Name", type: "text" },
    { key: "Purpose", label: "Zweck", type: "textarea" },
    { key: "Endpoint", label: "Endpoint (leer = keiner)", type: "nulltext" },
  ],
  InfraNode: [
    { key: "Resource", label: "Ressource", type: "text" },
    { key: "Provider", label: "Provider", type: "text" },
    { key: "Config", label: "Konfiguration (key=value pro Zeile)", type: "kv" },
  ],
  InvariantNode: [
    { key: "Description", label: "Beschreibung", type: "text" },
    { key: "Rule", label: "Regel", type: "invrule" },
  ],
  TestNode: [
    { key: "SpecRef", label: "Testet Spec", type: "ref", filterKind: "SpecNode" },
    { key: "Name", label: "Name", type: "text" },
    { key: "Derived", label: "Aus Spec abgeleitet", type: "check" },
  ],
};

const el = (tag, cls, text) => {
  const e = document.createElement(tag);
  if (cls) e.className = cls;
  if (text !== undefined) e.textContent = text;
  return e;
};

const labeled = (label, input) => {
  const wrap = el("label", "f-field");
  wrap.appendChild(el("span", "f-label", label));
  wrap.appendChild(input);
  return wrap;
};

const mkSelect = (options, value) => {
  const s = el("select", "f-input");
  for (const o of options) {
    const [val, lab] = Array.isArray(o) ? o : [o, o];
    const opt = el("option", null, lab);
    opt.value = val;
    if (val === value) opt.selected = true;
    s.appendChild(opt);
  }
  return s;
};

// Wiederholbare Zeilen (Kriterien, Beziehungen, Referenzen) mit +/−.
const rowList = (items, renderRow, makeEmpty, addLabel) => {
  const box = el("div", "f-rows");
  const rows = [];
  const addRow = (item) => {
    const row = el("div", "f-row");
    const reader = renderRow(row, item);
    const del = el("button", "f-del", "−");
    del.type = "button";
    del.onclick = () => {
      box.removeChild(row);
      rows.splice(rows.indexOf(reader), 1);
    };
    row.appendChild(del);
    rows.push(reader);
    box.insertBefore(row, addBtn);
  };
  const addBtn = el("button", "f-add", addLabel);
  addBtn.type = "button";
  addBtn.onclick = () => addRow(makeEmpty());
  box.appendChild(addBtn);
  for (const it of items) addRow(it);
  return { box, read: () => rows.map((r) => r()) };
};

export function buildForm(entry, entries, { idEditable = false } = {}) {
  const kase = entry.Payload.Case;
  const item = structuredClone(entry.Payload.Fields?.Item ?? {});
  const root = el("div", "f-form");

  const idInput = el("input", "f-input");
  idInput.value = entry.Id ?? "";
  idInput.disabled = !idEditable;
  idInput.placeholder = "eindeutige-id (a-z, 0-9, -, _)";
  root.appendChild(labeled("Id", idInput));

  const convSelect = mkSelect(CONVERGENCE, entry.Convergence);
  root.appendChild(labeled("Konvergenz", convSelect));

  const readers = {};
  const termIds = entries.filter((e) => e.Payload.Case === "TermNode").map((e) => e.Id);
  const allIds = entries.map((e) => e.Id);

  for (const f of schemas[kase] ?? []) {
    const val = item[f.key];
    switch (f.type) {
      case "text": {
        const i = el("input", "f-input");
        i.value = val ?? "";
        root.appendChild(labeled(f.label, i));
        readers[f.key] = () => i.value;
        break;
      }
      case "textarea": {
        const t = el("textarea", "f-input");
        t.rows = 3;
        t.value = val ?? "";
        root.appendChild(labeled(f.label, t));
        readers[f.key] = () => t.value;
        break;
      }
      case "select": {
        const s = mkSelect(f.options, val ?? f.options[0]);
        root.appendChild(labeled(f.label, s));
        readers[f.key] = () => s.value;
        break;
      }
      case "check": {
        const c = el("input");
        c.type = "checkbox";
        c.checked = !!val;
        root.appendChild(labeled(f.label, c));
        readers[f.key] = () => c.checked;
        break;
      }
      case "nulltext": {
        const i = el("input", "f-input");
        i.value = val ?? "";
        root.appendChild(labeled(f.label, i));
        readers[f.key] = () => (i.value.trim() ? i.value : null);
        break;
      }
      case "csv": {
        const i = el("input", "f-input");
        i.value = (val ?? []).join(", ");
        root.appendChild(labeled(f.label, i));
        readers[f.key] = () => i.value.split(",").map((s) => s.trim()).filter(Boolean);
        break;
      }
      case "lines": {
        const t = el("textarea", "f-input");
        t.rows = 3;
        t.value = (val ?? []).join("\n");
        root.appendChild(labeled(f.label, t));
        readers[f.key] = () => t.value.split("\n").map((s) => s.trim()).filter(Boolean);
        break;
      }
      case "kv": {
        const t = el("textarea", "f-input");
        t.rows = 3;
        t.value = Object.entries(val ?? {}).map(([k, v]) => `${k}=${v}`).join("\n");
        root.appendChild(labeled(f.label, t));
        readers[f.key] = () => Object.fromEntries(
          t.value.split("\n").map((l) => l.split("=")).filter((p) => p[0]?.trim())
            .map(([k, ...v]) => [k.trim(), v.join("=").trim()]));
        break;
      }
      case "ref": {
        const ids = f.filterKind
          ? entries.filter((e) => e.Payload.Case === f.filterKind).map((e) => e.Id)
          : allIds;
        const s = mkSelect(ids.length ? ids : [val ?? ""], val);
        root.appendChild(labeled(f.label, s));
        readers[f.key] = () => s.value;
        break;
      }
      case "refnull": {
        const s = mkSelect([["", "—"], ...allIds.map((i) => [i, i])], val ?? "");
        root.appendChild(labeled(f.label, s));
        readers[f.key] = () => (s.value ? s.value : null);
        break;
      }
      case "refs": {
        const { box, read } = rowList(
          val ?? [],
          (row, target) => {
            const s = mkSelect(allIds, target);
            row.appendChild(s);
            return () => s.value;
          },
          () => allIds[0] ?? "",
          "+ Referenz");
        root.appendChild(labeled(f.label, box));
        readers[f.key] = read;
        break;
      }
      case "relations": {
        const { box, read } = rowList(
          val ?? [],
          (row, rel) => {
            const kindSel = mkSelect(REL_KINDS, rel?.Case ?? "RelatesTo");
            const targetSel = mkSelect(termIds.length ? termIds : [""], rel?.Fields?.Item);
            row.appendChild(kindSel);
            row.appendChild(targetSel);
            return () => ({ Case: kindSel.value, Fields: { Item: targetSel.value } });
          },
          () => null,
          "+ Beziehung");
        root.appendChild(labeled(f.label, box));
        readers[f.key] = () => read().filter((r) => r.Fields.Item);
        break;
      }
      case "invrule": {
        const RULES = [
          ["SpecsNeedTests", "Jede Spec braucht einen Test"],
          ["CriticalRisksNeedMitigation", "Kritische Risiken brauchen Mitigation"],
          ["TermsNeedDefinition", "Begriffe brauchen Definition"],
          ["IdPrefix", "Id-Präfix pro Knotenart"],
        ];
        const current = typeof val === "string" ? val : val?.Case ?? "SpecsNeedTests";
        const ruleSel = mkSelect(RULES, current);
        const kindIn = el("input", "f-input"); kindIn.placeholder = "Knotenart (z. B. term)";
        const prefIn = el("input", "f-input"); prefIn.placeholder = "Präfix (z. B. term-)";
        if (typeof val === "object" && val?.Fields) {
          kindIn.value = val.Fields.kind ?? ""; prefIn.value = val.Fields.prefix ?? "";
        }
        const extra = el("div", "f-row");
        extra.appendChild(kindIn); extra.appendChild(prefIn);
        const syncVis = () => { extra.style.display = ruleSel.value === "IdPrefix" ? "" : "none"; };
        ruleSel.onchange = syncVis; syncVis();
        const box = el("div", "f-rows");
        box.appendChild(ruleSel); box.appendChild(extra);
        root.appendChild(labeled(f.label, box));
        readers[f.key] = () =>
          ruleSel.value === "IdPrefix"
            ? { Case: "IdPrefix", Fields: { kind: kindIn.value.trim(), prefix: prefIn.value.trim() } }
            : ruleSel.value;
        break;
      }
      case "criteria": {
        const { box, read } = rowList(
          val ?? [],
          (row, c) => {
            const g = el("input", "f-input"); g.placeholder = "Given …"; g.value = c?.Given ?? "";
            const w = el("input", "f-input"); w.placeholder = "When …"; w.value = c?.When ?? "";
            const t = el("input", "f-input"); t.placeholder = "Then …"; t.value = c?.Then ?? "";
            row.classList.add("f-crit");
            row.appendChild(g); row.appendChild(w); row.appendChild(t);
            return () => ({ Given: g.value, When: w.value, Then: t.value });
          },
          () => null,
          "+ Kriterium");
        root.appendChild(labeled(f.label, box));
        readers[f.key] = () => read().filter((c) => c.Given || c.When || c.Then);
        break;
      }
    }
  }

  return {
    el: root,
    getValue() {
      const newItem = { ...item };
      for (const [key, read] of Object.entries(readers)) newItem[key] = read();
      return {
        Id: idInput.value.trim(),
        Payload: { Case: kase, Fields: { Item: newItem } },
        Convergence: convSelect.value,
      };
    },
  };
}
