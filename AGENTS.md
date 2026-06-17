# AI Agent Project Map

This file is working memory for agents changing the shared QuizCanners code. Keep it useful for fast orientation: where shared helpers live, which contracts matter, and which local patterns should be reused before adding new ones.

QuizCanners is shared infrastructure used by multiple Unity projects. Keep changes project-agnostic unless a project-specific caller explicitly requires an adapter outside this folder.

## Fast Orientation

| Need | Start Here | Notes |
| --- | --- | --- |
| PEGI/IPEGI inspectors and editor/runtime inspection UI | `Inspector/Scripts` | `IPEGI`, `Inspect`, `InspectInList`, enter/exit contexts, collection inspectors, icons, editor helpers, and player/editor inspector surfaces. |
| PEGI examples and usage clues | `Inspector/Examples and Documentation` | Use as reference before inventing a new inspection pattern. |
| Inspector assets | `Inspector/Resources` | Shared icons, styles, and resources used by the inspector layer. |
| Gate, change, and throttling helpers | `Utils/Logic Scripting` | `Gate.*` patterns for cached transitions, once-per-frame checks, and delayed/conditional work. |
| Rendering, materials, shaders, and render textures | `Utils/Rendering` | Material helpers, shader properties, render texture utilities, blits, and GPU-facing convenience code. |
| Serialization and persisted data helpers | `Utils/Serialization Utils` | Serializable dictionaries, preferences, domain reload helpers, CFG-like helpers, and persistence utilities. |
| Scene/singleton helpers | `Utils/Scene Exploring` | Singleton utilities, scene lookup helpers, and scene object support. |
| Timed/background flow helpers | `Utils/Timed Coroutines` | Time-sliced or coroutine-oriented utility code. |
| Migration and CFG encode/decode | `Migration` | Legacy/project data migration and serialization compatibility helpers. |

## Repository Layout

```text
Quiz-cAnners/
  AGENTS.md
  Inspector/
    Examples and Documentation/
    Resources/
    Scripts/
  Migration/
  Utils/
    Logic Scripting/
    Rendering/
    Scene Exploring/
    Serialization Utils/
    Timed Coroutines/
```

## Shared Library Boundary

This folder should stay reusable across projects.

- Do not add dependencies on a specific game, scene, workflow, MAVLink subsystem, project singleton, or project asset path.
- If a project needs special behavior, put the adapter in that project's `Assets/_PROJECT` area and keep QuizCanners generic.
- Avoid project-specific logging text, menu labels, or assumptions about render pipeline, input system, gameplay state, or persistent data layout.
- Treat public APIs and serialized fields as cross-project contracts. Rename or change semantics only when callers are checked.
- Prefer small compatibility-preserving additions over broad rewrites. This code has a larger blast radius than a local feature folder.

## PEGI And Inspector Surface

PEGI/IPEGI is a normal development and debug surface in these projects, not throwaway editor UI.

Before adding inspection code:

- Look for nearby `IPEGI`, `Inspect`, `InspectInList`, `Enter_Inspect`, and `CollectionInspectorMeta` patterns.
- Keep inspection readable and close to the data owner.
- Avoid causing hidden allocations, scans, reflection, disk reads, shader rebuilds, or scene searches on every inspector repaint.
- Inspector buttons may trigger expensive work, but passive labels and foldouts should not.
- Keep editor-only APIs behind editor guards or inside editor assemblies/folders when required by Unity.

## Performance Bias

QuizCanners utilities are often used from hot paths. A small helper can become expensive when called by every object, every frame, every inspector repaint, or every render pass.

- Treat helpers used by `Update`, rendering, UI refresh, inspectors, serialization, and collection enumeration as performance-sensitive until proven otherwise.
- Avoid LINQ, boxing, closure allocations, reflection, temporary lists, string churn, and repeated shader/property lookups in hot helpers.
- Do not hide expensive work in cheap-looking getters, implicit conversions, `ToString`, inspector labels, or convenience wrappers.
- Names like `Build`, `Regenerate`, `Scan`, `Find`, `Bake`, `Recalculate`, `Encode`, and `Decode` should make expensive work obvious.
- Prefer cached IDs, stable buffers, explicit dirty flags, and `Gate.*` patterns where the surrounding code already follows that shape.
- Keep cache ownership and invalidation local and visible.

## Loud Contracts

Shared utilities should fail loudly when a caller breaks a required contract.

- Avoid broad null checks that silently hide broken prefab, inspector, shader, or lifecycle wiring.
- Prefer exceptions, assertions, visible errors, or `QuizCanners.Utils.QcLog.ChillLogger` one-time diagnostics when native code or Unity APIs would otherwise fail silently.
- Silent `return` paths are acceptable only for expected optional behavior. When the state is invalid by construction, make the failure visible.
- Do not clamp or fallback away bad values if the caller should have produced valid data. Fix the source or make the contract explicit.
- When adding guardrails, name the deeper ownership or lifecycle issue that remains.

## Rendering And Shader Helpers

Rendering utilities are shared by shader-heavy projects and can affect editor, mobile, and build behavior.

- Reuse existing `ShaderProperty`, `MaterialInstancer`, render texture, blit, and material helper patterns before adding new wrappers.
- Cache shader property IDs. Avoid repeated string lookups in per-frame or per-render code.
- Be explicit about material ownership: shared material, instantiated material, pooled material, or temporary material.
- Be explicit about render texture ownership, dimensions, format, lifetime, release path, and rebuild trigger.
- Check both CPU-side setup and GPU-side consumption when diagnosing shader issues.
- Be careful with shader-side clamps and fallbacks; they can hide bad C# values.
- Keep mobile/build compatibility in mind. Editor rendering success does not guarantee device success.

## Serialization And Migration

Serialization helpers affect persisted project data and legacy compatibility.

- Preserve backward compatibility unless the task explicitly includes a migration.
- Keep persisted source truth clear. Runtime caches should have an owner and invalidation path.
- Avoid changing encoded field names, keys, or decode defaults casually.
- When decoding legacy data, fail visibly for corrupt required data but tolerate genuinely optional older fields.
- Keep migrations deterministic and locally testable where practical.

## Gates, Singletons, And Scene Helpers

Common QuizCanners helpers should make lifecycle and update timing easier to see.

- Use existing `Gate.*` helpers for repeated state/change checks before hand-rolling tracking fields.
- Singleton and scene lookup helpers should not hide repeated expensive searches in cheap-looking accessors.
- If a lookup is cached, make invalidation and scene/domain reload behavior clear.
- Avoid adding implicit global state unless there is already a clear local convention for it.

## Editing Conventions

These are not a replacement for reading nearby code. They are recurring local preferences:

- Prefer established QuizCanners patterns over new abstractions.
- Add abstractions only when they remove real duplication or clarify an existing repeated pattern.
- Keep comments sparse and useful; explain surprising contracts, lifecycle requirements, or performance traps.
- Keep utility APIs boring and predictable. Clever convenience here can become confusing everywhere.
- Respect runtime/editor assembly boundaries and Unity serialization limitations.
- Do not mix unrelated cleanup with behavior changes in shared infrastructure.

## Known Failure Patterns

Use these as diagnosis smells, not blanket bans:

- A broad helper hides an allocation or scene scan that becomes a frame-time spike.
- Inspector UI triggers rebuilds every repaint.
- A null check masks broken required wiring and delays the real error.
- Shader-side clamping hides incorrect material or C# state.
- A singleton accessor performs a scene search repeatedly.
- A serialization default makes corrupted required data look valid.
- A migration helper changes behavior for current data while trying to support old data.
- A utility created for one project quietly imports assumptions that break another project.

## Updating This File

Keep this file short and structural.

Good additions:

- new shared subsystem entry points
- recurring helper names agents should search first
- local rules for performance-sensitive helpers
- links to deeper `AGENTS.md` files if a subfolder grows its own instructions

Avoid project-specific architecture notes here. Put those in the consuming project's `AGENTS.md`.
