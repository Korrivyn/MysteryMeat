# Phase 1 – OnInitialise Audit

## Current Lifecycle Responsibilities
- **OnInitialise**
  - Calls `EnsureInitialisation(null)` to spin up the logger, register the preference menus, and prime the debug helper.
  - Defers asset bundle loading until an activation context is supplied, logging the deferral through `DebugLogSystem`.
- **OnPostActivate**
  - Re-invokes `EnsureInitialisation(mod)` so the asset bundle is located, preloaded, and text mesh sprite resources are wired.
  - Logs the ASCII banner when readiness checks succeed, registers enabled cards, and subscribes to runtime events in a guarded block.

## Guarding Runtime Hooks
- `EnsureInitialisation` returns `false` when either preferences or assets are unavailable; `OnPostActivate` treats this as a hard stop for registrations and reports the failure at error level.
- `IsRuntimeReady()` centralises the readiness check so event handlers can verify dependencies before mutating game data.

## Notes for Later Phases
- Preference schema changes should continue to live inside `EnsureInitialisation` to guarantee they are available before activation.
- Any additional runtime hooks must confirm `IsRuntimeReady()` before subscribing to prevent null bundle usage.
