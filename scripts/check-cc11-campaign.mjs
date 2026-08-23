#!/usr/bin/env node

import {createHash} from "node:crypto";
import {existsSync, readFileSync, statSync} from "node:fs";
import {dirname, extname, join, normalize, resolve} from "node:path";
import {fileURLToPath} from "node:url";

const repo = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const pageDir = join(repo, "docs", "cc11");
const pagePath = join(pageDir, "index.html");
const campaignDir = join(repo, "campaigns", "cc11-alpha");
const errors = [];

const required = [
  pagePath,
  join(pageDir, "cc11.css"),
  join(pageDir, "cc11.js"),
  join(pageDir, "mark.svg"),
  join(pageDir, "apple-touch-icon.png"),
  join(pageDir, "og-cc11.png"),
  join(pageDir, "vendor", "cascadia.woff2"),
  join(pageDir, "media", "cc11-launch-film.mp4"),
  join(pageDir, "media", "cc11-launch-vertical.mp4"),
  join(pageDir, "media", "cc11-launch-poster.png"),
  join(campaignDir, "README.md"),
  join(campaignDir, "COPY-DECK.md"),
  join(campaignDir, "CHANNEL-PLAN.md"),
  join(campaignDir, "QA-CHECKLIST.md"),
  join(campaignDir, "release-manifest.json"),
  join(campaignDir, "SHA256SUMS"),
];

for (const path of required) {
  if (!existsSync(path) || statSync(path).size === 0) {
    errors.push(`missing or empty artefact: ${path.slice(repo.length + 1)}`);
  }
}

const html = readFileSync(pagePath, "utf8");
const requiredCopy = [
  "<title>CC11 — A system that can show its work</title>",
  '<link rel="canonical" href="https://cong42.de/cc11/">',
  'content="https://cong42.de/cc11/"',
  "Private shadow alpha",
  "A release page should disclose the unfinished work.",
  'id="film-transcript"',
];
for (const copy of requiredCopy) {
  if (!html.includes(copy)) errors.push(`required page contract missing: ${copy}`);
}

if (/<script(?![^>]*\bsrc=)[^>]*>/i.test(html)) {
  errors.push("inline script is not allowed");
}
if (/<style\b/i.test(html) || /\sstyle\s*=/i.test(html)) {
  errors.push("inline styles are not allowed");
}

const refPattern = /\b(?:href|src|poster)="([^"]+)"/g;
for (const match of html.matchAll(refPattern)) {
  const value = match[1];
  if (
    value.startsWith("#") ||
    value.startsWith("https://") ||
    value.startsWith("mailto:") ||
    value.startsWith("data:")
  ) {
    continue;
  }
  const withoutFragment = value.split("#", 1)[0];
  const target = normalize(join(pageDir, withoutFragment));
  const resolved = extname(target) ? target : join(target, "index.html");
  if (!existsSync(resolved)) {
    errors.push(`broken local reference: ${value}`);
  }
}

const publicText = required
  .filter((path) => existsSync(path) && [".html", ".css", ".js", ".json", ".md", ".svg"].includes(extname(path)))
  .map((path) => readFileSync(path, "utf8"))
  .join("\n");

const denied = [
  ["private IPv4 or mesh address", /\b(?:10\.|100\.64\.|192\.168\.|172\.(?:1[6-9]|2\d|3[01])\.)\d{1,3}(?:\.\d{1,3}){1,2}\b/],
  ["private key", /BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY/],
  ["bearer token", /\bBearer\s+[A-Za-z0-9._~-]{16,}/i],
  ["runtime secret path", /(?:^|[/\s])\.env(?:$|[/\s])/m],
  ["private runtime path", /\/opt\/cc10\//],
];
for (const [label, pattern] of denied) {
  if (pattern.test(publicText)) errors.push(`public campaign contains ${label}`);
}

const manifest = JSON.parse(readFileSync(join(campaignDir, "release-manifest.json"), "utf8"));
if (
  manifest.schema !== "cdd.public-campaign.v1" ||
  manifest.status !== "release-candidate" ||
  manifest.canonicalUrl !== "https://cong42.de/cc11/"
) {
  errors.push("campaign release manifest is inconsistent");
}

const checksumPath = join(campaignDir, "SHA256SUMS");
for (const line of readFileSync(checksumPath, "utf8").trim().split("\n")) {
  const [expected, relative] = line.split(/\s+/, 2);
  const path = join(repo, relative);
  const actual = createHash("sha256").update(readFileSync(path)).digest("hex");
  if (actual !== expected) errors.push(`checksum mismatch: ${relative}`);
}

if (errors.length) {
  for (const error of errors) console.error(`ERROR ${error}`);
  process.exit(1);
}

console.log(JSON.stringify({
  ok: true,
  artefacts: required.length,
  page: "docs/cc11/index.html",
  campaign: manifest.campaign,
  release: manifest.release,
}, null, 2));
