# UGUI Playable Commit Conventions

This document is the single authority for commit boundaries and commit messages
in this repository. It applies to new commits; do not rewrite existing history
only to make old commits conform.

Adapted from the EncosyTower commit conventions: same format and principles,
with topics mapped to the package's assemblies (`LibraryName.Feature.Support`,
see [PROJECT-CONVENTIONS.md](PROJECT-CONVENTIONS.md)).

## 1. Commit Boundaries

- Each commit must have one coherent, reviewable purpose and one topic.
- Split unrelated features, tooling, samples, vendored-library changes, and
  version metadata into separate commits.
- Keep tests, documentation, and assets with the change they directly validate,
  explain, or use.
- A feature commit must compile on its own: include its code, any new asmdef and
  `csc.rsp`, and the assets it needs.
- Agent-only files (`.claude/`, `CLAUDE.md`, `AGENTS.md`, `.agents/`, `Works/`) are never committed to this
  repository; they live in a separate private repository.
- Keep changes to vendored EncosyTower code (`Packages/com.laicasaane.encosy-tower`,
  `Plugins/`) separate from package code, even when made for the same feature.
- Porting EncosyTower or TweenPlayables code into the package is a package
  commit; include the `THIRD-PARTY-NOTICES.md` entry in the same commit.
- Preserve unrelated tracked and untracked work. Stage explicit paths and inspect
  the staged diff before committing.

### Unity-specific rules

- Commit every asset together with its `.meta` file. Add or delete both in the
  same commit, and never commit an orphan `.meta`.
- Files created outside the Editor have no `.meta` until Unity imports them.
  Let Unity import them before committing, otherwise GUIDs diverge between
  machines.
- Moving or renaming an asset must keep its `.meta` GUID. Move it in Unity, or
  move the `.meta` along with the file.
- Never commit `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, generated
  `*.csproj` / `*.slnx`, build outputs, keystores, or secrets.
- Do not commit code that fails Unity compilation unless the Project Owner
  explicitly accepts it.

## 2. Subject Format

Use this format:

```text
<Topic>: <brief imperative summary>
```

The release-version commit is the only standard exception:

```text
Version <version>
```

Subject rules:

- Maximum length is 70 characters, including the topic and colon.
- Use the most specific stable topic that owns the change.
- Start the summary with a lowercase imperative verb, such as `add`, `fix`,
  `remove`, `rename`, `refactor`, `update`, `tune`, or `rebuild`.
- Describe the outcome, not the files touched or the work performed.
- Do not end the subject with a period.
- Do not combine topics in the subject.
- `WIP` is acceptable only as a temporary local checkpoint. Reword or squash it
  before the commit becomes part of shared or release history.

Examples:

```text
UguiPlayable: add CanvasGroup alpha track
UguiPlayable: fix relative mode drift when scrubbing backwards
UguiPlayable.Editor: add ease curve preview to clip inspector
Tests: cover RectTransform track blending
Samples: add popup open/close timeline sample
Project: remove Entities and Latios packages
EncosyTower.PubSub: fix handler order for equal priority
Version 0.1.0
```

## 3. Topic Selection

Choose topics by ownership and behavior, not only by directory.

| Topic | Use |
|---|---|
| `Project` | Repository-wide configuration, `Packages/manifest.json` and lock file, `ProjectSettings/`, CI, root `README.md` / `LICENSE`, and `docs/` |
| `Agents` | Only in the private agent-files repository: `.claude/`, `AGENTS.md`, `CLAUDE.md`, `.agents/`, `Works/`. Never used in this repository |
| `UguiPlayable` | The runtime assembly `ApexionGame.UguiPlayable`, plus package metadata (`package.json`, package README, `THIRD-PARTY-NOTICES.md`) when not part of a release |
| `UguiPlayable.Editor` | Changes confined to `ApexionGame.UguiPlayable.Editor` |
| `UguiPlayable.<Feature>` | A future feature assembly `ApexionGame.UguiPlayable.<Feature>`, and `UguiPlayable.<Feature>.Editor` for its editor assembly |
| `Tests` | The package test assemblies, or test-only work without a narrower owner |
| `Samples` | Canonical samples under `Assets/Samples/UGUI Playable/` and development content under `Assets/ApexionGame/` |
| `EncosyTower` | Vendored EncosyTower package changes spanning several modules, or upgrading the vendored version |
| `EncosyTower.<Module>` | A change to one vendored EncosyTower module, e.g. `EncosyTower.PubSub` |
| `EncosyTower.SourceGen` | Changes to vendored generator sources under `Plugins/SourceGenerator` or shipped generator DLLs |
| `Version` | Package version bump, `Samples~` deployment and the release section of `CHANGELOG.md` |

Package topics are the assembly name without the `ApexionGame.` prefix,
following `LibraryName.Feature.Support`. New track families (Graphics, Layout,
Text, Audio, …) are sub-folders of an existing assembly, so they use that
assembly's topic. Name the feature in the subject, e.g.
`UguiPlayable: add Slider value track`.

Apply these rules when choosing between related topics:

- Use the assembly topic for its code, owned assets, and owned documentation.
  Use it for a change that touches the runtime and editor assemblies together.
- Use `<Assembly>.Editor` only when the change is confined to the editor assembly.
- Use `Tests` for changes to the test assemblies, including tests that
  accompany one assembly's change.
- Use `Project` for root documentation (including these conventions) unless a
  narrower feature topic clearly owns it.
- For a new feature, derive the topic from its new assembly name.

## 4. Commit Body

Omit the body when the subject fully explains the commit. When more context is
needed:

- Leave one blank line after the subject.
- Use concise `- <change>` bullets instead of prose paragraphs.
- Describe behavior, intent, compatibility, or a non-obvious reason, such as
  which EncosyTower pattern was ported, or how the behaviour differs from
  TweenPlayables.
- Do not list filenames or narrate the implementation process.
- Mention breaking behavior explicitly. In particular, call out changes to
  **public API** or **serialized fields** of shipped assets, which affect users'
  projects and timelines.

Example:

```text
UguiPlayable: blend overlapping Graphic color clips by weight

- Mix clips in linear color space, then write once per frame
- Restore the bound Graphic's original color when the graph stops
- Match TweenPlayables output for single-clip tracks
```

Commits carry no `Co-Authored-By` or other attribution trailer, AI-assisted
ones included. The message ends with the last body bullet.

## 5. Release Commit Series

Keep release outputs separate from their source changes. When applicable, use
this order:

1. Commit package code, samples, tests, and project changes by topic.
2. Commit vendored EncosyTower updates under their `EncosyTower` topics.
3. Commit the `package.json` version, the `Samples~` deployment and the
   `CHANGELOG.md` release section as `Version <version>`, then tag
   `v<version>`.

Skip a step when the release does not change that output.
