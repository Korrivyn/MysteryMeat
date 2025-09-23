# Phase 1 Audit – OnInitialise vs. OnPostActivate

## Summary of Current Responsibilities
| Action | Current Location | Classification | Dependencies / Order Constraints | Notes |
| --- | --- | --- | --- | --- |
| Emit ASCII art banner through `DebugLogSystem.LogInfo`. | `OnInitialise()` | Post-activation | Needs `DebugLogSystem.Initialise` to have captured a real `KitchenLogger`. | Executes before the logger is created, so the banner is currently dropped. Intended for celebratory post-activation output.
| Resolve the primary asset bundle via `mod.GetPacks<AssetBundleModPack>()` (with fallback exception). | `OnPostActivate()` | Initialization | Requires a `Mod` context to enumerate packs; everything else that touches assets relies on this bundle. | First heavy-duty operation in `OnPostActivate`; failure aborts activation.
| Acquire the KitchenLib logger through `InitLogger()`. | `OnPostActivate()` | Initialization | None, but `DebugLogSystem.Initialise` depends on the logger existing. | Sets `Mod.Logger` for reuse across helpers.
| Wire up the debug helper with `DebugLogSystem.Initialise(Logger, () => ActiveDebugLogLevel)`. | `OnPostActivate()` | Initialization | Requires `Logger`; later reads of `ActiveDebugLogLevel` expect `PrefManager` to be initialised. | Currently runs before preferences, so the delegate returns `Off` until the manager is created.
| Preload bundle textures via `Bundle.LoadAllAssets<Texture2D>()`. | `OnPostActivate()` | Initialization | Requires `Bundle`. | Helps avoid late asset lookups; safe to run as soon as the bundle is available.
| Preload bundle sprites via `Bundle.LoadAllAssets<Sprite>()`. | `OnPostActivate()` | Initialization | Requires `Bundle`. | Same as textures; should accompany other bundle priming work.
| Load custom `TMP_SpriteAsset` (`"GrindMeat"`). | `OnPostActivate()` | Initialization | Requires `Bundle`. | Acts as setup for TextMeshPro emoji support.
| Append sprite asset to `TMP_Settings.defaultSpriteAsset.fallbackSpriteAssets`. | `OnPostActivate()` | Initialization | Requires sprite asset loaded and TextMeshPro settings to be accessible. | Ensures the game can resolve the meat grinder emoji.
| Clone default TMP material and assign it to the custom sprite asset. | `OnPostActivate()` | Initialization | Requires sprite asset and `TMP_Settings.defaultSpriteAsset.material`. | Guarantees isolation from shared material mutations.
| Load the sprite texture (`"GrindMeatTex"`) and assign it to the cloned material. | `OnPostActivate()` | Initialization | Requires `Bundle` and cloned material. | Finalises sprite rendering setup; tightly coupled with prior sprite steps.
| Instantiate `PreferenceSystemManager`. | `OnPostActivate()` | Initialization | Requires mod identifiers; later preference access (`ActiveDebugLogLevel`) depends on this manager. | Foundation for all preference registrations.
| Build helper arrays via `IntArrayGenerator` for 0–100% sliders. | `OnPostActivate()` | Initialization | Requires `IntArrayGenerator`. | Supplies the volume option lists; must precede menu option registration.
| Define debug log level value/label arrays. | `OnPostActivate()` | Initialization | None beyond constant enum values. | Input to the debug preference option.
| Configure the preference menu hierarchy (`AddLabel`, `AddOption`, `AddSubmenu`, etc.). | `OnPostActivate()` | Initialization | Requires `PrefManager` and generated arrays. | Populates audio, card, and debug settings with defaults.
| Register the pause-menu preferences via `PrefManager.RegisterMenu(...)`. | `OnPostActivate()` | Initialization | Requires completed menu definition. | Makes the configuration accessible in-game; expected to occur before preferences are queried elsewhere.
| Conditionally register `CautiousCrowdCard`. | `OnPostActivate()` | Post-activation | Requires `PrefManager` to fetch user choice; depends on KitchenLib systems being ready to accept new GDOs. | Directly tied to gameplay data, so it should remain in the activation phase.
| Conditionally register `MessyMurderCard`. | `OnPostActivate()` | Post-activation | Same as above. | Same reasoning as the previous card.
| Conditionally register `PersistentCorpsesCard`. | `OnPostActivate()` | Post-activation | Same as above. | Same reasoning as the previous card.
| Subscribe to `Events.BuildGameDataEvent`. | `OnPostActivate()` | Post-activation | Requires KitchenLib events to be live; handler relies on `Bundle`. | Last-mile hook: waits for game data creation before mutating it.
| Extend `ItemReferences.Mince` with a kneading-derived process inside the event handler. | `BuildGameDataEvent` handler (registered in `OnPostActivate`) | Post-activation | Handler executes when game data exists; requires `GDOUtils` lookups to succeed. | Modifies core item behaviour; must execute after the database is built.
| Register stab SFX clips through `SetupSFX`. | `BuildGameDataEvent` handler | Post-activation | Needs `Bundle`, access to `gameData.ReferableObjects`, and event timing. | Ensures clip collection exists before audio IDs are used elsewhere.
| Register poison SFX clip through `SetupSFX`. | `BuildGameDataEvent` handler | Post-activation | Same as above. | Maintains parity with other sound events.
| Register alert SFX clip through `SetupSFX`. | `BuildGameDataEvent` handler | Post-activation | Same as above. | Completes audio registration set.

## Logging and Debug Preference Observations
- `OnInitialise()` calls `DebugLogSystem.LogInfo` before any logger has been initialised. Because `DebugLogSystem` resolves `Mod.Logger` lazily and `InitLogger()` does not run until `OnPostActivate()`, the ASCII banner is suppressed instead of appearing at startup. Moving banner emission to a point after `DebugLogSystem.Initialise` would allow the preference-aware logger to render it.
- The debug helper honours three levels:
  - **Off** (`0`): suppresses info, warning, and verbose logs; errors still emit without stack traces.
  - **On** (`1`): allows info/warning output and attaches stack traces for diagnostic clarity.
  - **Verbose** (`2`): keeps the previous behaviour and adds explicit verbose messages.
- `DebugLogSystem.Initialise` captures a delegate to `ActiveDebugLogLevel`. Until `PrefManager` is constructed, that accessor returns `Off`, so early log calls from activation steps will default to the quietest level. Once preferences finish loading, subsequent evaluations will respect the stored selection.

## Potential Risks When Rebalancing Responsibilities
- **Asset bundle access in `OnInitialise`:** `OnPostActivate(Mod mod)` currently supplies the `mod` handle used to enumerate `AssetBundleModPack` instances. We need to confirm whether the base class exposes an equivalent accessor during `OnInitialise` or if the handle must be cached earlier to avoid regressions.
- **Preference availability for early debug level reads:** If `PrefManager` is moved forward, we must ensure the preference backing store can be initialised prior to activation without triggering duplicate registrations or missing menu attachments.
- **TextMeshPro asset manipulation timing:** Relocating sprite asset configuration to `OnInitialise` should be validated against Unity’s loading lifecycle to ensure `TMP_Settings.defaultSpriteAsset` is ready and does not require the game data context present during activation.
- **Audio registration dependencies:** `SetupSFX` relies on both the asset bundle and the runtime `GameData` container. Only the bundle work should shift earlier; the actual `GameData` mutation must remain tied to a post-activation event where the referable registries exist.
- **Card registration ordering:** `AddGameDataObject<T>` calls may depend on KitchenLib’s activation pipeline. Moving them too early could bypass necessary initialization steps or execute before preferences set their defaults.

## Clarifications Needed for Phase 2
- What is the recommended way to access asset bundles during `OnInitialise` when the `mod` parameter is unavailable? Should we cache the `Mod` instance earlier or use a KitchenLib helper?
- Does KitchenLib guarantee that preference registration in `OnInitialise` will still surface menus in the pause screen, or must menu registration occur after activation?
- Are there side effects if `AddGameDataObject` is invoked before `OnPostActivate`, or is activation the earliest safe hook for new game data objects?
- Should the celebratory banner respect the debug level (only display when `DebugLogLevel` ≥ `On`), or is it expected to show regardless of verbosity settings?
