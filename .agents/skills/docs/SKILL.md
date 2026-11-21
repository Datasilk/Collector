# Docs conventions

How to write/maintain `Docs/` documentation.

## Structure

- `Docs/README.md` — the table of contents; every new doc gets linked.
- One `*.md` per system at the root; big systems get a folder of
  per-component docs (`Docs/Journal/Module-*.md`, `Docs/Chat/Tool-*.md`,
  `Docs/Web-Scraper/Section-*.md`).
- Naming: `Module-{Module-Name}.md`, `Tool-{Tool-Name}.md`,
  `Section-{Section-Name}.md` — human-readable link labels in the parent
  TOC ("Gather Research", not "gather-research").

## Format — every file

```
# {Title}

[Home]({../}README.md) > {Parent chain} > {Page}

{one-line plain-English summary of what this is}
```

Breadcrumb goes on line 3 — ancestors are links, current page is plain
text. Exactly one H1 per file.

## Rules

- Document what the code actually does — read the source, never invent
  behavior. Thin areas say so honestly.
- Prefer tables for settings/fields/commands, short bullet lists for
  flows, code fences for JSON shapes and directory trees.
- Update docs when the system changes — stale docs are worse than none.
