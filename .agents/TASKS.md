# TODO — Collector Chat Quality Improvements

Findings from live test runs. Implement top-down; move completed items to DONE.md.

## Pending

- [ ] **Off-topic "library" ambiguity sources pass the relevance gate.**
  Alexandria run integrated mdpls.org (Miami-Dade Public Library homepage)
  and lapl.org (LA Public Library homepage) as sources for a gap query
  about surviving texts. Homepage junk with zero article content. The
  gate needs to reject institutional homepages / ambiguous-keyword hits.
  File: `Helpers/SearchHelper.cs` (relevance filter)

- [ ] **UI-chrome images pass the gallery floor.** Britannica's
  star-ai.svg chatbot icon and an author headshot landed in the Alexandria
  galleries. Filters needed: SVG/icon-class assets, author/profile photos,
  images under a min byte size or with chrome-y URL paths
  (/chatbot/, /profile_photos/, /avatar/).
  File: `Tools/GatherResearch/WebPageHelper.cs` (WideImagesOnlyAsync)

- [x] ~~**Duplicate-key dump JSON crashes page integration.**~~ — FIXED
  (2026-11-21): model emitted `images` twice in one section; JsonObject
  threw "duplicate key" on access and the whole Alexandria final step
  failed to save. `ParseModelJson` rebuilds via JsonDocument (last-wins);
  all three parsedJson read sites routed through it. Tests: 100/100.
  File: `Tools/AddEditJournalEntryTool.cs`

- [~] **Parser fabricates comments to fill the schema.** The Alexandria run
  integrated 379 comments — the Britannica dump shows User115…User130+
  repeating two identical comment bodies, a clear LLM fill-the-schema
  hallucination. Fix landed: ParseSections dedupes comments on
  case/space-normalized body text (real threads never repeat verbatim).
  Needs live re-verification on a comments-bearing page. Residual: parser
  prompt could still emit `comments: []` more honestly.
  Files: `Tools/AddEditJournalEntryTool.cs` (ParseSections)

- [~] **Footer chrome lands as subtopics.** "References:" and "Related Posts"
  (article-footer link lists) became subtopics on the Alexandria run.
  Fix landed: IsBoilerplateSection now strips trailing `: . - — –` before
  matching, and "related posts"/"post navigation"/"recent articles"/
  "share this"/"subscribe"/"follow us"/"sign up"/"log in"/"about us"
  joined the boilerplate set. Needs live re-verification.
  File: `Tools/AddEditJournalEntryTool.cs` (ParseSections)

- [ ] **Near-duplicate / mangled topics from topic-assign.** Alexandria
  topics include "Scope and Scope" (garbled), "Content" vs
  "Organization and Content" (overlap), and "History and Legacy" vs
  "History and Legacy of the Library of Alexandria" (dupes). The topic
  assigner should merge near-identical topic titles; a post-assign
  dedupe/normalization pass could catch this deterministically.
  File: `Helpers/ResearchBackfill.cs` or topic-assign call site

- [ ] **POST-form search engines carry session-bound tokens.** biorxiv's
  discovered engine captured `form_build_id: form-XIGiOka…` — Drupal mints a
  new one per session, so the replay goes stale. On a replay 403/invalid-
  form failure, re-fetch the page and re-extract the build id (or drop
  hidden session fields marked volatile).
  File: `Helpers/SearchHelper.cs`

- [ ] **Verify scientific discovery on more sites.** arXiv, PMC, and
  bioRxiv registered engines (GET form, POST w/ `?term=`, Drupal POST form).
  Try nature.com / science.org / Semantic Scholar (JS-driven — exercises the
  probe tier) next.

## Recently observed

- [x] ~~ContentSections fallback produced one nav-chrome blob~~ — FIXED +
  VERIFIED (2026-11-21). Fallback now runs on raw elements with ancestry
  chrome filtering; Terracotta run's Wikipedia scrape integrated 79 real
  paragraphs. See DONE.md.

- [x] ~~U+FFFD replacement chars in body text~~ — FALSE ALARM (2026-11-21).
  `Ptolemy�s` was a console-codepage display artifact: dumps and entry.json
  contain clean UTF-8 (`’` U+2019, `–` U+2013 verified via proper decode).
  No data corruption anywhere in the scrape→parse→integrate path.
