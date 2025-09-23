**Context**

Repository: `/workspace/MysteryMeat`
Primary file: `Mod.cs`

Goals:
- Move all heavy initialisation (logger, asset bundle, preferences) into `OnInitialise`.
- Restrict `OnPostActivate` to finalisation tasks (banner emission, runtime hooks) that assume initialisation completed.
- Ensure debug logging honours the Off/On/Verbose preference, including stack traces for On and Verbose levels via `DebugLogSystem`.
- Maintain Microsoft .NET conventions and keep comments JavaDoc-like, focusing on purpose and order of operations.

Community guidance (collated from Reddit and Discord PlateUp modding threads):
- `OnInitialise` is safe for synchronous resource loading and preference setup.
- `OnPostActivate` should only handle activation success messaging and runtime hooks.
- Event subscriptions depending on game data should verify assets and preferences are ready before wiring handlers.

**Tasks**
1. Refactor lifecycle methods
   - Ensure `OnInitialise` prepares the logger, debug helper, asset bundle, and preferences without relying on activation.
   - Update `OnPostActivate` to call `EnsureInitialisation(mod)` and skip runtime hooks if initialisation fails, logging a high-priority error when that occurs.
   - Emit the ASCII banner only after activation and only when the logger is ready.

2. Consolidate conditional logic
   - Replace the repeated card registration `if` statements with a collection-driven loop (e.g., tuples containing preference IDs and registration delegates).
   - Apply similar consolidation to other repetitive guards while respecting readability and avoiding excessive returns.

3. Strengthen runtime guards
   - Subscribe to `Events.BuildGameDataEvent` only when the asset bundle and preferences are initialised.
   - Gate `SetupSFX` operations behind bundle and game-data availability checks, logging warnings or errors when dependencies are missing.
   - Consider extracting helper methods to clarify readiness checks before performing runtime registrations.

4. Documentation & comments
   - Add summary comments to each method and purpose-driven comments before non-trivial logic blocks (especially guards).
   - Update or create documentation under `Docs/PhaseNotes` describing audit findings and current responsibilities so future phases have historical context.

5. Testing & reporting
   - Run available validation commands (e.g., `dotnet build`) and record outcomes for the project manager.

Deliverables: Clean commit with refactored code, updated comments, and refreshed documentation. Ensure `git status` is clean before handoff.
