# Work Family Conventions

A **work family** is one large piece of work (a system, a migration, a research question) carried through four
stages, each run by its own skill and each ending in files on disk. The files, not the chat history, hold the state,
so any session on any machine can pick the work up from `handoff.md`.

```
research ──► review ──► Owner decisions ──► plan ──► Owner approval ──► execute (one step at a time)
/research-family  /review-family   (chat)       /plan-task-family    (chat)       /execute-task-family
```

Small, one-session tasks don't need a family: keep using a single dated file in `docs/plans/`.

## 1. Layout

Families live in `Works/` at the repo root, split by stage. `<family>` is short kebab-case (`save-system`,
`level-editor`) and is the same name in both folders:

- `Works/Research/<family>/` holds the research, the design, the review record and `handoff.md`.
- `Works/Plan/<family>/` holds `plan.md` and `steps/`.

| File | Folder | Written by | Content |
|---|---|---|---|
| `handoff.md` | Research | every stage | Status line, file list, Owner decisions (D1, D2…), open points, next step. Always current. |
| `research-<topic>.md` | Research | research | Feasibility or survey of one question: verdict first, evidence after. One file per question. |
| `design.md` | Research | research, updated after decisions | Current design: requirements, structure, APIs, phasing, risks, open points. |
| `review-record.md` | Research | review | Review passes, corrections adopted, positions per question → decision, open disagreements. |
| `plan.md` | Plan | plan | Ordered step list with status, dependencies and acceptance checks. |
| `steps/NN-<slug>.md` | Plan | plan | One step: goal, files, verified APIs, tasks, acceptance, test checklist. |

Links between the two folders are relative: from `plan.md` they are `../../Research/<family>/handoff.md`, and from a
step file `../plan.md`.

Superseded drafts are deleted, not kept beside the current file; git history keeps them. Each file's content
follows its template: `handoff.md` in §2, the others in §3.

## 2. `handoff.md`

```markdown
# <family> family — handoff

Status (YYYY-MM-DD): <one line: stage reached, what exists, what does not>.

## Files
- `handoff.md` — this file.
- [design.md](design.md) — <one line>.

## Owner decisions (YYYY-MM-DD)
- D1 (O1) <topic>: **<decision>**. <constraints the Owner attached>.

## Open points for the Owner
1. <question> — options and positions in design.md §N.

## Next step
1. <exact next action, naming the skill and mode>.
```

Rules:
- Decisions are numbered once and never renumbered. `D5 (O3)` means decision 5 closed open point O3. A reversed
  decision gets a new number that names the old one ("D9 replaces D4").
- Only the Owner makes decisions. A skill records proposals as open points, never as decisions.
- Update the status line and the next step at the end of every stage and every executed step.

## 3. File templates

Every family file follows its template: same headings, same order. A section with nothing to say keeps its heading
and says `None.` so a reader can tell "checked, empty" from "forgotten". Sections may add sub-headings; they don't
drop or reorder the template's headings.

### 3.1 `research-<topic>.md`

```markdown
# <Topic> — research

- Family: `<family>`. Date: YYYY-MM-DD.
- Question: RQn — <the question, one sentence>.
- Sources: <project paths, package versions from Packages/manifest.json, upstream repo @ commit + license>.
- Decisions applied: <D-numbers this research assumes, or None>.

## Verdict

<Answer first, in a short paragraph: yes/no/partly, and how. Bold the one-line answer.>

## Findings

### 1. <Finding or blocker>

<What exists, what breaks, how to handle it. Evidence as `path:line`.>

## Options

| Option | Pros | Cons | Recommendation |
|---|---|---|---|

## Effort estimate

| Part | Size | Notes |
|---|---|---|

## Unverified / to check before planning

- <claim> — <how to check it>.
```

### 3.2 `design.md`

```markdown
# <Family title> — design

- Family: `<family>`. Date: YYYY-MM-DD. Research only: nothing was compiled or run (remove once code exists).
- Binding decisions: D1–Dn in [handoff.md](handoff.md). Research: <links>. Review: [review-record.md](review-record.md).
- Toolchain (verified in repo): Unity <version>, EncosyTower <version>, <packages that matter>.
- Names: undecided names are placeholders in angle brackets: `<Name>`.

## 1. Requirements

- R1: <requirement>. Source: <Owner request, issue, D-number>.

Research questions: RQ1 <question> → <research file or section>.

## 2. Structure

<Assemblies and folders per PROJECT-CONVENTIONS, EncosyTower modules used and why, data flow. A diagram if it helps.>

## 3. <Design part>

<One section per part: data, APIs, runtime flow, UI (PageFlows + MVVM), editor tooling, save data. Each states what
is decided (D-number) and what is proposed.>

## N-4. Phasing

1. <Phase>: <what ships, what it depends on>.

## N-3. Spike

<Only when unknowns must be measured first. Otherwise: None.>

| # | Measurement | Acceptance question |
|---|---|---|

## N-2. Risks

- <risk> — <mitigation>.

## N-1. Out of scope

- <item> — <why, and where it goes instead>.

## N. Open points for the Owner

1. **<Topic>**: <question>. Options: <A>, <B>. Recommendation: <A>, because <reason>.
```

### 3.3 `review-record.md`

```markdown
# <Family title> — review record

- Family: `<family>`. Date: YYYY-MM-DD.
- Reviewed: <files and their state, e.g. design.md after D1–D6>.
- Raw pass reports are not kept in the repo.

## Passes

| # | Pass | Questions | Model and effort | Verification |
|---|---|---|---|---|

## Corrections adopted

| # | Finding | Evidence | Pass |
|---|---|---|---|

## Questions, positions and decisions

| Question | Positions | Decision |
|---|---|---|

## Disagreements still open

| Topic | Position A | Position B |
|---|---|---|
```

### 3.4 `plan.md`

```markdown
# <Family title> — plan

- Family: `<family>`. Date: YYYY-MM-DD. Mode: NEW | UPDATE (YYYY-MM-DD: <what changed>).
- Implements: D1–Dn from [handoff.md](../../Research/<family>/handoff.md); design: [design.md](../../Research/<family>/design.md).
- Approval: awaiting Owner | approved YYYY-MM-DD.

## Steps

| NN | Step | Depends on | Commit topic | Status |
|---|---|---|---|---|
| 00 | [<title>](steps/00-<slug>.md) | — | <Topic> | todo |

Status values: `todo`, `in progress`, `done`, `blocked: <reason>`, `dropped: <reason>`.

## Risks per step

- NN: <risk> — <mitigation>.

## Deferred

- <item> — <why, and when it comes back>.

## Change log

- YYYY-MM-DD: <plan created | what UPDATE changed and why>.
```

### 3.5 `steps/NN-<slug>.md`

```markdown
# Step NN — <title>

- Family: `<family>`. Plan: [plan.md](../plan.md). Implements: <D-numbers>. Depends on: <steps or None>.

## Goal

<One paragraph: what is true after this step that wasn't before.>

## Files

| File | Change |
|---|---|
| `Packages/com.apexiongame.ugui-playable/ApexionGame.UguiPlayable.<Feature>/…` | create / modify |

## Verified APIs

| API | Signature | Source |
|---|---|---|
| `<Type.Member>` | `<signature>` | `path:line` |

## Tasks

1. <Task, small enough to check off>.

## Acceptance

- Compile: `unity-editor` recompile, console has no errors.
- Tests: <tests to add or run, or None>.
- Owner checklist (Play Mode):
  - [ ] <observable behaviour>.

## Out of scope

- <item>.

## Result

<Added by execute-task-family when the step ends: date, what changed, deviations from the plan and why,
verification evidence, commit SHA.>
```

## 4. Writing rules for all family files

- Verdict or answer first, evidence after.
- Cite evidence as `path:line`, package versions, or upstream commit hashes. Mark anything not re-read against the
  source as **Unverified**.
- Placeholders for undecided names go in angle brackets (`<SaveService>`); never present a working name as final.
- Dates are absolute (`2026-10-29`), never "today" or "last week".
- Write the docs in English, like the rest of `docs/`.
- Follow PROJECT-CONVENTIONS and CODING-CONVENTIONS for anything the design proposes (assembly names, package
  layout, public API surface, EncosyTower patterns to port checked with `encosy-tower-advisor`).

## 5. Models

Follow the Agent model selection table in `CLAUDE.md`: research, review, planning and code review run on Opus;
implementing an approved step runs on Sonnet with a self-contained brief, reviewed by Opus before it is reported done.

## 6. Commits

Each stage ends with a commit through the `commit` skill, topic `Agents` for family docs only (for example
`Agents: research save-system family`). Executed steps commit under the topic of the code they change.
