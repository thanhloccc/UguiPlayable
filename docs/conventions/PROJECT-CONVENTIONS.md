# UGUI Playable Project Conventions

This document is the tool-neutral authority for repository workflows and
project-level ownership rules of **UGUI Playable**, an open-source (MIT) Unity
package that adds Timeline tracks, clips and editor tooling for animating uGUI.
It is adapted from the EncosyTower project conventions. EncosyTower is kept in
this repository as a design reference and a development dependency (see §5),
but the shipped package never depends on it.

Minimum Unity version: **6000.3**.

## 1. Specialized Conventions

- For C# code, follow [CODING-CONVENTIONS.md](CODING-CONVENTIONS.md).
- Before staging or creating commits, follow
  [COMMIT-CONVENTIONS.md](COMMIT-CONVENTIONS.md).
- Large work (a new track family, an editor tool, a migration) is planned in work families, described in
  [WORK-FAMILY-CONVENTIONS.md](WORK-FAMILY-CONVENTIONS.md). The family files live in a private repository.
- Prior art: [AnnulusGames/TweenPlayables](https://github.com/AnnulusGames/TweenPlayables)
  (MIT, no longer maintained, IMGUI editors). It is a reference for scope and
  gaps, not a source to copy from without attribution (§5).

## 2. Repository Layout

The repository is a Unity development project. The product is the embedded
package under `Packages/`; everything else exists to develop, test and
demonstrate it.

```text
UguiPlayable/
├── Packages/
│   ├── com.apexiongame.ugui-playable/          the shipped package (§3)
│   ├── com.laicasaane.encosy-tower/            vendored reference + dev dependency (§5)
│   └── com.laicasaane.encosy-tower.dev-tools/  vendored dev tools
├── Plugins/                                    vendored EncosyTower generator/low-level sources (§5)
├── Assets/
│   ├── ApexionGame/                            development-only content (sandbox scenes, probes)
│   └── Samples/UGUI Playable/                  canonical sample sources (§4)
├── docs/conventions/                           these conventions
├── README.md, LICENSE                          repository-level docs (the changelog lives in the package)
└── ProjectSettings/, Packages/manifest.json    Unity project setup
```

`Assets/` never holds shipped code. Code that users receive lives in the
package; code that only helps development lives in `Assets/ApexionGame/` and
is never referenced by the package.

## 3. Package Layout and Assemblies

The package follows the EncosyTower package shape: each assembly lives in a
folder named exactly like the assembly, directly under the package root.

```text
Packages/com.apexiongame.ugui-playable/
├── package.json                                     name com.apexiongame.ugui-playable, unity 6000.3
├── README.md, LICENSE.md, CHANGELOG.md
├── THIRD-PARTY-NOTICES.md                           attribution for ported code (§5)
├── ApexionGame.UguiPlayable/                        runtime: tracks, clips, behaviours, mixers, easing
├── ApexionGame.UguiPlayable.Editor/                 UI Toolkit editors, drawers, preview, menus
├── ApexionGame.UguiPlayable.Tests.EditorMode/       EditMode tests
├── ApexionGame.UguiPlayable.Tests.PlayMode/         PlayMode tests (when needed)
└── Samples~/                                        release deployment output of the samples (§4)
```

### Naming

- Assembly name, root namespace and folder follow **`LibraryName.Feature.Support`**:
  - `LibraryName` is `ApexionGame.UguiPlayable`.
  - `Feature` is an optional PascalCase noun for a further split, added only
    when it earns its own assembly (for example an optional integration such
    as `ApexionGame.UguiPlayable.TextMeshPro`, if it ever needs separate
    version defines).
  - `Support` is an optional role: `Editor`, `Tests.EditorMode`, `Tests.PlayMode`.
- The folder name **is** the assembly name, never a short name and never a
  `Runtime/` layer.
- Namespace = assembly name + sub-folders, for example
  `ApexionGame.UguiPlayable.Tracks.Graphics`,
  `ApexionGame.UguiPlayable.Editor.Inspectors`.
- Group files inside an assembly by domain (track family, easing, preview),
  not by kind.
- A new assembly is a deliberate, reviewed decision; prefer a sub-folder in an
  existing assembly.

### Assembly setup

- Every asmdef has a sibling `csc.rsp` containing `-langversion:10`.
- Runtime asmdefs reference only Unity assemblies the package declares in
  `package.json` (`Unity.Timeline`, `UnityEngine.UI`, `Unity.TextMeshPro`).
  They never reference `EncosyTower.*` or any other vendored or development
  assembly.
- Editor asmdefs use `"includePlatforms": ["Editor"]` and `"autoReferenced": false`.
- Test asmdefs use `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`,
  `nunit.framework.dll` and `"autoReferenced": false`; EditMode tests also use
  `"includePlatforms": ["Editor"]`.
- Reference assemblies by name.
- Optional integrations use asmdef `versionDefines` (one per asmdef that needs
  them), never a project-wide scripting define the user must set by hand.

### Dependencies

- `Editor` → runtime. Tests → runtime and `Editor`. No cycles.
- `package.json` declares every Unity package the package uses, with the
  lowest version that works on Unity 6000.3. Adding a dependency is a reviewed
  decision; it is a cost every user pays.

### Public API

- The package is consumed by people outside this repository. Everything
  `public` is a contract: name it deliberately, document it with XML
  comments, and keep it stable across minor versions.
- Prefer `internal` for anything not meant for users. Tests use
  `InternalsVisibleTo` declared in the assembly's `AssemblyInfo.cs`.
- Serialized field names of shipped `ScriptableObject`/`PlayableAsset`/
  `TrackAsset` types are part of the user's data. Renaming one requires
  `[FormerlySerializedAs]` and a CHANGELOG entry.
- Breaking changes are listed under a **Breaking** heading in `CHANGELOG.md`.

## 4. Samples

The package ships **one sample** (Owner decision 2026-11-05), laid out like EncosyTower's samples.

- **Canonical source:** `Assets/Samples/UGUI Playable/<version>/ApexionGame.UguiPlayable.Samples/`. This is the path
  where the Package Manager imports a sample, so the development project sees the sample exactly as users do. It is
  tracked by Git.
- **Contents:**
  - asmdef `ApexionGame.UguiPlayable.Samples` (`autoReferenced: false`) and `csc.rsp`;
  - scene `ugui-playable-sample.unity`;
  - `Prefabs/Screens|Popups|Overlays/`: page prefabs in the hierarchy standard (memory `ui-hierarchy-standard`);
  - `Timelines/`: shared transition timelines, with tracks named by role;
  - `Scripts/`, `Art/`.
  - Namespace `ApexionGame.UguiPlayable.Samples`.
- **Release output:** `Packages/com.apexiongame.ugui-playable/Samples~/ApexionGame.UguiPlayable.Samples/`, listed in
  `package.json` `samples` as `{ displayName, description, path }`.
- **Where to edit:** make sample changes in `Assets/Samples`, never in `Samples~`. At release:
  - copy the canonical folder into `Samples~`, replacing the previous content;
  - move the `<version>` folder to the new version;
  - review the complete `Samples~` diff.
- **Building:** build sample scenes and UI with unity CLI commands, not with builder scripts, so later hand edits
  survive. Timeline tracks and clips, which the CLI can't create, are set up by one-off `eval` calls.
- **Position channels:** in shared show/hide timelines, use Absolute targets or From current, not Relative.
  Relative re-bases each time a director's graph is rebuilt, so a hide that doesn't return to its start drifts.
- **Dependencies:** the sample depends only on the package and the Unity packages it declares; its EventSystem
  gets its input module at runtime (Input System or Input Manager), and no module is serialized. TextMeshPro text
  needs TMP Essential Resources.

## 5. EncosyTower: Reference, Not Dependency

- `Packages/com.laicasaane.encosy-tower` (embedded package, version in its
  `package.json`), `Packages/com.laicasaane.encosy-tower.dev-tools` and
  `Plugins/` are **vendored upstream code**. They stay in the development
  project as the design reference and for development tooling. The shipped
  package must not reference them.
- Follow EncosyTower's design: assembly shape, editor style (UI Toolkit,
  model/problems/sections split, `<Tool>StyleSheetPaths`, Dark/Light USS),
  validation and `ThrowHelper` patterns, coding conventions.
- When the package needs functionality EncosyTower has (an editor asset loader,
  a UI Toolkit helper, a collection, a guard), **port it** into the package:
  - copy only what is used, as `internal`, into the owning assembly under the
    package's namespace;
  - keep a header comment naming the source file and EncosyTower version, for
    example `// Ported from EncosyTower 0.1.9-preview.5: EncosyTower.Editor/EditorAPI.cs`;
  - record it in `THIRD-PARTY-NOTICES.md` with the MIT notice
    (Copyright (c) 2026 Laicasaane).
  - The same rule applies to code taken from TweenPlayables
    (Copyright (c) AnnulusGames, MIT).
- Do not edit vendored code. If a change is truly needed, it is a deliberate,
  minimal change agreed with the Project Owner and committed separately under
  an `EncosyTower` topic.
- `Samples~` of the vendored package is a read-only reference. Imported
  EncosyTower samples under `Assets/Samples/Encosy Tower` are study material
  only; the UGUI Playable package never uses them.

## 6. Unity-Owned Files

- Root `.csproj` files and `UguiPlayable.slnx` are generated by Unity and
  ignored by Git.
- Do not edit generated project files manually. Let Unity refresh their contents
  and project membership, then reload dependent tooling.
- Never run `dotnet build`, `dotnet restore`, `dotnet test`, `dotnet clean`,
  `dotnet format`, MSBuild, NuGet restore, or equivalent commands against
  Unity-generated `.csproj`, `.sln`, or `.slnx` files unless the Project Owner
  explicitly authorizes the exact command and target.
- Do not run untargeted .NET, MSBuild, or NuGet commands from the repository root
  when they could automatically select a Unity-generated project or solution.
- A general request to build, test, validate, or diagnose does not grant this
  permission. Use Unity-owned compilation and test workflows instead.
- This restriction does not prohibit commands against tracked, standalone .NET
  workspaces that never touch Unity-generated projects, such as
  `Plugins/SourceGenerator/EncosyTower.SourceGen.slnx` and agent harnesses kept
  outside the repository (which build into the system temp directory).
  Task-specific validation restrictions still apply.
- Assets created outside the Editor get their `.meta` only after Unity imports
  them. Never hand-write `.meta` files. Files inside the embedded package need
  `.meta` files too.

## 7. Editor UI

- Editor UI is UI Toolkit only. IMGUI (`OnGUI`, `OnInspectorGUI`,
  `EditorGUILayout`) needs Project Owner approval. Timeline's own extension
  points that are IMGUI-based (for example `ClipEditor` / `TrackEditor`
  drawing callbacks) are allowed where Timeline offers no UI Toolkit
  alternative; keep that code minimal and isolated.
- Editor code lives in `ApexionGame.UguiPlayable.Editor`.
- Menu items:
  - windows under `Window/UGUI Playable/<Tool>`;
  - asset creation under `Assets/Create/UGUI Playable/<Asset>`;
  - Timeline tracks appear through `[TrackClipType]` / `[TrackBindingType]`
    and their display names, grouped as `UGUI Playable/<Group>/<Track>`.
- USS classes use the `ugp-` prefix with BEM:
  `ugp-<feature>-<block>__<element>--<modifier>`.
- Put each tool's UXML/USS in
  `ApexionGame.UguiPlayable.Editor/<Tool>/StyleSheets/`, with Dark and Light
  variants.
- Use a `<Tool>StyleSheetPaths` owner with a `ROOT` composed from the package
  root (`Packages/com.apexiongame.ugui-playable/…`) and explicit `UXML_PATH` /
  `USS_PATH` or qualified variants.
- Asset loading (`LoadAsset<T>`, `GetOrLoadAsset<T>(ref field, path)`,
  `LoadStyleSheet` / `GetOrLoadStyleSheet`) is ported from
  `EncosyTower.Editor.EditorAPI` into an internal editor helper of this package
  (§5). Keep dark/light caches separate. Cached Unity assets must reload when
  destroyed, not only when their managed reference is null. Required-asset
  failures keep a path-bearing diagnostic; do not silently render an empty
  Inspector.
- Each assembly keeps its guards in its own internal `ThrowHelper`.
  Type-specific invariants stay with their owner. Do not broaden visibility or
  add helpers without actual reuse.
- Preserve `.meta` GUIDs when moving existing assets.

## 8. Validation Evidence

- Treat these as separate evidence:
  - static searches;
  - Roslyn semantic analysis, including offline generator harnesses kept outside
    the repository;
  - Unity compilation and domain reload;
  - Unity Test Runner results (EditMode, PlayMode);
  - Timeline behaviour checked in the Editor (scrubbing, preview, Play Mode);
  - player builds.
- Each validation layer proves only its own result. Do not substitute one layer
  for another or claim a check that was not run.
- Follow task-specific validation restrictions. Report the exact checks and
  results, plus anything intentionally not run or left for the Project Owner.

## 9. Release

- Version lives in `package.json` (SemVer; `-preview.N` before 1.0).
- A release updates, in order: code and samples, `Samples~` deployment (§4),
  `CHANGELOG.md`, then the version commit (COMMIT-CONVENTIONS §5).
- Users install through the Git URL
  `https://github.com/<owner>/<repo>.git?path=Packages/com.apexiongame.ugui-playable`
  (optionally `#<tag>`). Tags are the bare version, with no `v` prefix
  (`0.1.0-preview.2`), which OpenUPM reads as SemVer.
