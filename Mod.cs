using System;
using KitchenLib;
using KitchenLib.Logging.Exceptions;
using KitchenMods;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KitchenLogger = KitchenLib.Logging.KitchenLogger;
using KitchenLib.Interfaces;
using KitchenLib.Event;
using KitchenLib.Utils;
using KitchenLib.References;
using KitchenData;
using TMPro;
using System.Collections.Generic;
using KitchenLib.Preferences;
using PreferenceSystem.Generators;
using PreferenceSystem;
using KitchenMysteryMeat.Customs.Cards;
using KitchenMysteryMeat.Enums;
using KitchenMysteryMeat.Systems;
using KitchenMysteryMeat.Systems.Logging;

namespace KitchenMysteryMeat
{
    public class Mod : BaseMod, IModSystem, IAutoRegisterAll
    {
        public const string MOD_GUID = "com.quackandcheese.mysterymeat";
        public const string MOD_NAME = "Mystery Meat";
        public static new readonly Version ModVersion = new Version(0, 3, 1);
        public static string ModVersionString => ModVersion.ToString();
        public const string MOD_AUTHOR = "QuackAndCheese";
        public const string MOD_GAMEVERSION = ">=1.2.7";

        internal static AssetBundle Bundle;
        internal static KitchenLogger Logger;

        /// <summary>
        /// Represents the expected asset bundle file name that contains the mod content.
        /// </summary>
        private const string AssetBundleFileName = "mod.assets";

        /// <summary>
        /// Tracks whether BuildGameData has been subscribed for the current activation cycle.
        /// </summary>
        private static bool _buildGameDataSubscribed;

        /// <summary>
        /// Tracks whether the enabled cards have been registered during the current activation cycle.
        /// </summary>
        private static bool _cardsRegistered;

        /// <summary>
        /// Records whether runtime hook registration deferral has already been logged.
        /// </summary>
        private static bool _runtimeHooksDeferredLogged;

        /// <summary>
        /// Gets the ASCII art banner displayed when the mod is initialised.
        /// </summary>
        private static string ModLoadedBanner
        {
            get
            {
                string[] bannerLines =
                {
                    @"",
                    @"      _____         _                   ⠀⠀⠀⠀⠀⠀⠀⢀⣠⡶⠶⣦⣄⠀ ⠀⢀⣴⣿⣷⡄",
                    @"     |     |_ _ ___| |_ ___ ___ _ _    ⠀⠀⠀⠀⠀⠀⣠⣴⣿⣿⡇⡖⠂⠙⠗⣠⣾⣿⣿⣿⣥⣀",
                    @"     | | | | | |_ -|  _| -_|  _| | |  ⠀⢀⣀⣠⣤⣶⣿⣿⣿⣿⣿⣇⢣⠀⣠⣾⣿⣿⣿⣿⣿⣿⣿⠇",
                    @"     |_|_|_|_  |___|_| |___|_| |_  |  ⢰⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡌⢧⠘⠿⠟⠛⣉⠉",
                    @"           |___|               |___|  ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣌⠳⣄⠀⠀⣿⡀",
                    @"         _____         _     ⠀⠀⠀⠀⠀     ⢸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣌⣉⣁⡿",
                    @"        |     |___ ___| |_     ⠀  ⢀⣤⣤⣤⣤⡀⢻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⠟⠛⠉",
                    @"        | | | | -_| .'|  _|      ⠀⢻⣿⣿⣿⣿⣷⡀⠹⣿⣿⣿⣿⣿⣿⣿⣿⠟⠋⠁",
                    $"        |_|_|_|___|__,|_|   ⠀     ⠀⠉⢩⣿⣿⣿⠋⠀⠈⠻⢿⣿⣿⣿⠋⠁⠀⠀⠀⠀v{ModVersion}",
                    $"           by {MOD_AUTHOR}       ⠀⠸⣿⡿⠁⠀⠀⠀⠀⠀⠈⠉",
                    @"",
                    @""
                };
                return string.Join(Environment.NewLine, bannerLines);
            }
        }

        /// <summary>
        /// Initialises a new instance of the Mystery Meat mod with the configured metadata.
        /// </summary>
        public Mod() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, ModVersionString, MOD_GAMEVERSION, Assembly.GetExecutingAssembly())
        {
        }

        public static SoundEvent StabSoundEvent;
        public static SoundEvent PoisonSoundEvent;
        public static SoundEvent AlertSoundEvent;


        internal static PreferenceSystemManager PrefManager;
        public const string MEAT_GRINDER_VOLUME_ID = "meatGrinderVolume";
        public const string STAB_VOLUME_ID = "stabVolume";
        public const string ALERT_VOLUME_ID = "alertVolume";
        public const string SUSPICION_VOLUME_ID = "suspicionVolume";
        public const string CAUTIOUS_CROWD_ENABLED_ID = "cautiousCrowdEnabled";
        public const string MESSY_MURDER_ENABLED_ID = "messyMurderEnabled";
        public const string PERSISTENT_CORPSES_ENABLED_ID = "persistentCorpsesEnabled";
        public const string DEBUG_LOG_LEVEL_ID = "debugLogLevel";

        /// <summary>
        /// Gets the active debug logging level for the mod.
        /// </summary>
        public static DebugLogLevel ActiveDebugLogLevel
        {
            get
            {
                DebugLogLevel activeLevel = DebugLogLevel.Off;

                // Ensures preference lookups only occur when the manager has been initialised.
                if (PrefManager != null)
                {
                    int storedLevel = PrefManager.Get<int>(DEBUG_LOG_LEVEL_ID);

                    // Validates the stored preference before casting it to the debug log level enum.
                    if (Enum.IsDefined(typeof(DebugLogLevel), storedLevel))
                    {
                        activeLevel = (DebugLogLevel)storedLevel;
                    }
                }

                return activeLevel;
            }
        }

        /// <summary>
        /// Resets cached runtime state so every activation begins from a clean slate.
        /// </summary>
        private void ResetInitialisationState()
        {
            // Guard: detach from BuildGameData when a previous activation already registered the handler.
            if (_buildGameDataSubscribed)
            {
                Events.BuildGameDataEvent -= OnBuildGameData;
                _buildGameDataSubscribed = false;
            }

            // Guard: release any loaded asset bundle before attempting to resolve a fresh instance.
            if (Bundle != null)
            {
                Bundle.Unload(true);
            }

            Bundle = null;
            Logger = null;
            PrefManager = null;
            _cardsRegistered = false;
            _runtimeHooksDeferredLogged = false;

            // Guard: clear the cached activation context so reflection resolves the current mod instance.
            _cachedActivationContext = null;
            _activationContextWarningLogged = false;

            // Reset the debug log system to discard stale logger references between activations.
            DebugLogSystem.Initialise(null, () => ActiveDebugLogLevel);
        }

        /// <summary>
        /// Handles initial mod setup by preparing core systems and queuing runtime hook registration.
        /// </summary>
        protected override void OnInitialise()
        {
            // Reset static caches to ensure reactivations begin from a known-good state.
            ResetInitialisationState();

            // Prepare logging and preferences so subsequent operations can query configuration safely.
            bool coreReady = EnsureCoreInitialisation();

            // Resolve the activation context so assets can be loaded during initialisation rather than activation.
            KitchenMods.Mod activationContext = ResolveActivationContext();

            // Attempt to load the asset bundle immediately to keep runtime hooks from accessing a null bundle.
            bool assetsReady = EnsureAssetBundle(activationContext);

            // Guard: report when the logger or preferences are unavailable during initialisation.
            if (!coreReady)
            {
                DebugLogSystem.LogError("Mystery Meat failed to initialise its core systems; runtime registrations have been skipped to avoid inconsistent state.");
            }

            // Guard: report asset loading failures only when the activation context was available but initialisation still failed.
            if (!assetsReady && activationContext != null)
            {
                DebugLogSystem.LogError("Mystery Meat failed to initialise its asset bundle; runtime registrations have been skipped to avoid inconsistent state.");
            }

            // Attempt to register runtime hooks so they activate automatically once dependencies are available.
            TryRegisterRuntimeHooks();
        }

        /// <summary>
        /// Handles per-frame updates for the mod.
        /// </summary>
        protected override void OnUpdate()
        {
            // Guard: retry runtime hook registration until both cards and game data subscriptions succeed.
            if (!_cardsRegistered || !_buildGameDataSubscribed)
            {
                TryRegisterRuntimeHooks();
            }
        }

        /// <summary>
        /// Handles post-activation duties by ensuring assets are available, completing runtime registration, and displaying the mod banner.
        /// </summary>
        /// <param name="mod">The activation context supplied by KitchenLib.</param>
        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            // Capture the activation context in case initialisation occurs before the framework exposes the instance.
            CacheActivationContext(mod);

            // Prepare core services again in case dependencies became available only after activation.
            bool coreReady = EnsureCoreInitialisation();

            // Resolve the asset bundle once the activation context has been provided explicitly.
            bool assetsReady = EnsureAssetBundle(mod);

            // Guard: surface core readiness issues so diagnostics remain visible after activation.
            if (!coreReady)
            {
                DebugLogSystem.LogError("Mystery Meat activation completed without initialising its core systems; runtime registration will continue retrying until dependencies load.");
            }

            // Guard: surface asset readiness issues once activation has supplied the bundle context.
            if (!assetsReady && mod != null)
            {
                DebugLogSystem.LogError("Mystery Meat activation completed without resolving its asset bundle; runtime registration will continue retrying until the bundle loads.");
            }

            // Attempt to register runtime hooks now that activation has supplied every dependency.
            TryRegisterRuntimeHooks();

            // Guard: report when the runtime is not ready so the banner reflects the activation status accurately.
            if (!IsRuntimeReady())
            {
                DebugLogSystem.LogWarning("Mystery Meat activation completed without fully initialising runtime dependencies; review earlier logs for details.");
            }
            else
            {
                // Emit the banner once activation has succeeded so players receive confirmation of the mod state.
                DebugLogSystem.LogInfo(ModLoadedBanner);
            }
        }

        /// <summary>
        /// Ensures the mod logger and preference manager are available before runtime hooks are registered.
        /// </summary>
        /// <returns>True when both the logger and preference manager have been initialised successfully.</returns>
        private bool EnsureCoreInitialisation()
        {
            // Initialise the logger when it has not yet been created so diagnostics can be emitted reliably.
            if (Logger == null)
            {
                Logger = InitLogger();
            }

            // Synchronise the debug helper with the logger reference even when the logger already existed.
            DebugLogSystem.Initialise(Logger, () => ActiveDebugLogLevel);

            // Guard: surface when the logger could not be initialised so fallback logging expectations are clear.
            if (Logger == null)
            {
                DebugLogSystem.LogError("Mystery Meat failed to initialise its dedicated logger; Unity console output will be used as a fallback.");
            }

            // Guard: construct the preference manager exactly once so menu registration is idempotent.
            if (PrefManager == null)
            {
                try
                {
                    IntArrayGenerator intArrayGenerator = new IntArrayGenerator();

                    // Generates percentage values for shared audio preference controls.
                    intArrayGenerator.AddRange(0, 100, 10, null, delegate (string prefKey, int value)
                    {
                        return $"{value}%";
                    });

                    int[] zeroToHundredPercentValues = intArrayGenerator.GetArray();
                    string[] zeroToHundredPercentStrings = intArrayGenerator.GetStrings();
                    int[] debugLogLevelValues =
                    {
                        (int)DebugLogLevel.Off,
                        (int)DebugLogLevel.On,
                        (int)DebugLogLevel.Verbose
                    };
                    string[] debugLogLevelLabels =
                    {
                        "Off",
                        "On",
                        "Verbose"
                    };
                    intArrayGenerator.Clear();

                    PrefManager = new PreferenceSystemManager(MOD_GUID, MOD_NAME);

                    PrefManager
                        .AddLabel("Mystery Meat")
                        .AddSpacer()
                        .AddSubmenu("Audio Settings", "AudioSubmenu")
                            .AddLabel("Audio Settings")
                            .AddSpacer()
                            .AddLabel("Meat Grinder Volume")
                            .AddOption<int>(
                                MEAT_GRINDER_VOLUME_ID,
                                50,
                                zeroToHundredPercentValues,
                                zeroToHundredPercentStrings)
                            .AddLabel("Stab Volume")
                            .AddOption<int>(
                                STAB_VOLUME_ID,
                                50,
                                zeroToHundredPercentValues,
                                zeroToHundredPercentStrings)
                            .AddLabel("Suspicion Volume")
                            .AddOption<int>(
                                SUSPICION_VOLUME_ID,
                                50,
                                zeroToHundredPercentValues,
                                zeroToHundredPercentStrings)
                            .AddLabel("Alert Volume")
                            .AddOption<int>(
                                ALERT_VOLUME_ID,
                                50,
                                zeroToHundredPercentValues,
                                zeroToHundredPercentStrings)
                            .AddSpacer()
                            .AddSpacer()
                            .SubmenuDone()
                        .AddSubmenu("Card Settings", "CardSubmenu")
                            .AddLabel("Card Settings")
                            .AddInfo("Any changes require a restart.")
                            .AddLabel("Cautious Crowd")
                            .AddOption<bool>(
                                CAUTIOUS_CROWD_ENABLED_ID,
                                true,
                                [true, false],
                                ["Enabled", "Disabled"]
                            )
                            .AddLabel("Messy Murder")
                            .AddOption<bool>(
                                MESSY_MURDER_ENABLED_ID,
                                true,
                                [true, false],
                                ["Enabled", "Disabled"]
                            )
                            .AddLabel("Persistent Corpses")
                            .AddOption<bool>(
                                PERSISTENT_CORPSES_ENABLED_ID,
                                true,
                                [true, false],
                                ["Enabled", "Disabled"]
                            )
                            .AddSpacer()
                            .AddSpacer()
                            .SubmenuDone()
                        // Adds debug settings for controlling log output verbosity.
                        .AddSubmenu("Debug Settings", "DebugSubmenu")
                            .AddLabel("Debug Settings")
                            .AddOption<int>(
                                DEBUG_LOG_LEVEL_ID,
                                (int)DebugLogLevel.Off,
                                debugLogLevelValues,
                                debugLogLevelLabels)
                            .AddSpacer()
                            .AddSpacer()
                            .SubmenuDone()
                    .AddSpacer()
                    .AddSpacer();

                    PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
                }
                catch (Exception ex)
                {
                    PrefManager = null;

                    DebugLogSystem.LogError($"Mystery Meat failed to initialise preferences: {ex}");
                }
            }

            bool isReady = Logger != null && PrefManager != null;
            return isReady;
        }

        /// <summary>
        /// Ensures the mod asset bundle is available and configured once activation has supplied the context.
        /// </summary>
        /// <param name="mod">The activation context that exposes asset bundles.</param>
        /// <returns>True when the asset bundle is ready for use.</returns>
        private bool EnsureAssetBundle(KitchenMods.Mod mod)
        {
            // Determine whether the asset bundle still requires loading so redundant lookups are avoided.
            bool assetsReady = Bundle != null;

            // Guard: attempt to load the asset bundle only when it has not been cached already.
            if (!assetsReady)
            {
                // Guard: warn when the activation context has not been supplied yet.
                if (mod == null)
                {
                    DebugLogSystem.LogWarning("Mystery Meat skipped asset bundle loading because the activation context was not provided.");
                }
                else
                {
                    AssetBundle resolvedBundle = ResolveMysteryMeatAssetBundle(mod);

                    // Guard: confirm the asset bundle has been found before attempting to cache it.
                    if (resolvedBundle != null)
                    {
                        Bundle = resolvedBundle;

                        // Preload commonly used assets so runtime lookups occur without additional I/O.
                        Bundle.LoadAllAssets<Texture2D>();
                        Bundle.LoadAllAssets<Sprite>();

                        TMP_SpriteAsset spriteAsset = Bundle.LoadAsset<TMP_SpriteAsset>("GrindMeat");

                        // Guard: register the sprite asset as a fallback only once to avoid duplicate references.
                        if (spriteAsset != null)
                        {
                            TMP_SpriteAsset defaultSpriteAsset = TMP_Settings.defaultSpriteAsset;

                            // Guard: validate the default sprite asset before attempting to configure fallback resources.
                            if (defaultSpriteAsset == null)
                            {
                                DebugLogSystem.LogWarning("Mystery Meat could not configure TextMeshPro fallbacks because the default sprite asset is unavailable.");
                            }
                            else
                            {
                                List<TMP_SpriteAsset> fallbackSpriteAssets = defaultSpriteAsset.fallbackSpriteAssets;

                                // Guard: initialise the fallback list when TextMeshPro leaves it null on specific builds.
                                if (fallbackSpriteAssets == null)
                                {
                                    fallbackSpriteAssets = new List<TMP_SpriteAsset>();
                                    defaultSpriteAsset.fallbackSpriteAssets = fallbackSpriteAssets;
                                }

                                // Guard: register the sprite asset as a fallback only once to avoid duplicate references.
                                if (!fallbackSpriteAssets.Contains(spriteAsset))
                                {
                                    fallbackSpriteAssets.Add(spriteAsset);
                                }

                                // Guard: configure the sprite asset only when the default material is available.
                                if (defaultSpriteAsset.material != null)
                                {
                                    spriteAsset.material = UnityEngine.Object.Instantiate(defaultSpriteAsset.material);

                                    // Guard: ensure the grind meat texture exists before applying it to the sprite material.
                                    Texture2D grindMeatTexture = Bundle.LoadAsset<Texture2D>("GrindMeatTex");
                                    if (grindMeatTexture != null)
                                    {
                                        spriteAsset.material.mainTexture = grindMeatTexture;
                                    }
                                    else
                                    {
                                        DebugLogSystem.LogWarning("Mystery Meat could not locate the GrindMeat texture within the asset bundle.");
                                    }
                                }
                                else
                                {
                                    DebugLogSystem.LogWarning("Mystery Meat skipped sprite material configuration because the default TextMeshPro material is unavailable.");
                                }
                            }
                        }
                        else
                        {
                            DebugLogSystem.LogWarning("Mystery Meat could not locate the GrindMeat sprite asset inside the bundle.");
                        }

                        assetsReady = true;
                    }
                    else
                    {
                        DebugLogSystem.LogError("Mystery Meat could not locate its asset bundle during activation; audio registration will be skipped.");
                    }
                }
            }

            assetsReady = Bundle != null;
            return assetsReady;
        }

        /// <summary>
        /// Resolves the Mystery Meat asset bundle from the provided activation context in a deterministic manner.
        /// </summary>
        /// <param name="mod">The activation context exposing the available asset bundles.</param>
        /// <returns>The resolved asset bundle when located; otherwise, null.</returns>
        private AssetBundle ResolveMysteryMeatAssetBundle(KitchenMods.Mod mod)
        {
            AssetBundle resolvedBundle = null;

            // Gather all available asset bundles so signature matching can occur deterministically.
            List<AssetBundle> candidateBundles = mod
                .GetPacks<AssetBundleModPack>()
                .SelectMany(pack => pack.AssetBundles ?? Enumerable.Empty<AssetBundle>())
                .Where(bundle => bundle != null)
                .ToList();

            // Guard: attempt to match the expected bundle file name before falling back to signature checks.
            resolvedBundle = candidateBundles
                .FirstOrDefault(bundle => string.Equals(bundle.name, AssetBundleFileName, StringComparison.OrdinalIgnoreCase));

            // Guard: fall back to signature matching when the canonical bundle name could not be located.
            if (resolvedBundle == null)
            {
                resolvedBundle = candidateBundles.FirstOrDefault(BundleContainsSignatureAsset);
            }

            // Guard: provide a warning when no candidate satisfied the heuristics but bundles were still present.
            if (resolvedBundle == null && candidateBundles.Count > 0)
            {
                DebugLogSystem.LogWarning("Mystery Meat detected asset bundles but none matched the expected signature; defaulting to the first candidate.");
                resolvedBundle = candidateBundles[0];
            }

            return resolvedBundle;
        }

        /// <summary>
        /// Determines whether the provided asset bundle contains the expected signature assets for Mystery Meat.
        /// </summary>
        /// <param name="bundle">The asset bundle to inspect.</param>
        /// <returns>True when the bundle contains known Mystery Meat assets; otherwise, false.</returns>
        private static bool BundleContainsSignatureAsset(AssetBundle bundle)
        {
            bool containsSignature = false;

            // Guard: ensure the bundle reference is valid before inspecting its contents.
            if (bundle != null)
            {
                string[] assetNames = bundle.GetAllAssetNames();

                // Guard: attempt to locate the GrindMeat assets which uniquely identify the bundle.
                if (assetNames != null && assetNames.Any(assetName => assetName.IndexOf("grindmeat", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    containsSignature = true;
                }
            }

            return containsSignature;
        }

        /// <summary>
        /// Registers gameplay cards based on the active preference configuration.
        /// </summary>
        private void RegisterEnabledCards()
        {
            // Guard: ensure preferences are available before querying configuration values.
            if (PrefManager == null)
            {
                DebugLogSystem.LogWarning("Mystery Meat skipped card registration because preferences have not been initialised.");
            }
            else
            {
                // Collates enabled card registrations so duplicate preference checks are avoided.
                (bool IsEnabled, Action RegisterCard, string PreferenceId)[] cardRegistrations =
                {
                    (PrefManager.Get<bool>(CAUTIOUS_CROWD_ENABLED_ID), () => AddGameDataObject<CautiousCrowdCard>(), CAUTIOUS_CROWD_ENABLED_ID),
                    (PrefManager.Get<bool>(MESSY_MURDER_ENABLED_ID), () => AddGameDataObject<MessyMurderCard>(), MESSY_MURDER_ENABLED_ID),
                    (PrefManager.Get<bool>(PERSISTENT_CORPSES_ENABLED_ID), () => AddGameDataObject<PersistentCorpsesCard>(), PERSISTENT_CORPSES_ENABLED_ID)
                };

                foreach ((bool IsEnabled, Action RegisterCard, string PreferenceId) cardRegistration in cardRegistrations)
                {
                    // Guard: only register the card when the associated preference is enabled.
                    if (cardRegistration.IsEnabled)
                    {
                        cardRegistration.RegisterCard();

                        // Provide verbose diagnostics that capture which preference enabled the card.
                        DebugLogSystem.LogVerbose($"Registered Mystery Meat card using preference '{cardRegistration.PreferenceId}'.");
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to register runtime hooks such as cards and game data subscriptions when dependencies are ready.
        /// </summary>
        private void TryRegisterRuntimeHooks()
        {
            bool runtimeReady = IsRuntimeReady();

            // Guard: defer registration until both assets and preferences are available.
            if (!runtimeReady)
            {
                // Guard: log the deferral only once per activation to avoid spamming verbose output.
                if (!_runtimeHooksDeferredLogged)
                {
                    DebugLogSystem.LogVerbose("Mystery Meat deferred runtime hook registration because assets or preferences are still initialising.");
                    _runtimeHooksDeferredLogged = true;
                }
            }
            else
            {
                _runtimeHooksDeferredLogged = false;

                // Guard: register cards only once per activation to avoid duplicate game data objects.
                if (!_cardsRegistered)
                {
                    RegisterEnabledCards();
                    _cardsRegistered = true;
                }

                // Ensure BuildGameData subscriptions occur once runtime dependencies are confirmed ready.
                SubscribeToBuildGameDataEvent();
            }
        }

        /// <summary>
        /// Subscribes to the BuildGameData event once so asset-driven modifications can be applied.
        /// </summary>
        private void SubscribeToBuildGameDataEvent()
        {
            // Guard: avoid duplicate subscriptions which would apply modifications repeatedly.
            if (_buildGameDataSubscribed)
            {
                return;
            }

            // Guard: ensure runtime dependencies exist before wiring the subscription to prevent null references later.
            if (PrefManager == null)
            {
                DebugLogSystem.LogWarning("Mystery Meat deferred BuildGameData subscription because preferences are unavailable.");
            }
            else
            {
                Events.BuildGameDataEvent += OnBuildGameData;
                _buildGameDataSubscribed = true;

                DebugLogSystem.LogVerbose("Mystery Meat subscribed to the BuildGameData event.");
            }
        }

        /// <summary>
        /// Indicates whether runtime hooks can be safely registered based on asset and preference readiness.
        /// </summary>
        /// <returns>True when both the asset bundle and preference manager have been initialised.</returns>
        private static bool IsRuntimeReady()
        {
            bool hasAssets = Bundle != null;
            bool hasPreferences = PrefManager != null;

            bool isReady = hasAssets && hasPreferences;
            return isReady;
        }

        /// <summary>
        /// Handles BuildGameData event invocations once activation has succeeded.
        /// </summary>
        /// <param name="sender">The event source supplied by the framework.</param>
        /// <param name="args">The event arguments containing the game data instance.</param>
        private void OnBuildGameData(object sender, BuildGameDataEventArgs args)
        {
            // Guard: ensure runtime dependencies remain available before applying game data modifications.
            if (!IsRuntimeReady())
            {
                DebugLogSystem.LogWarning("BuildGameDataEvent triggered before Mystery Meat finished initialising; the handler execution has been skipped.");
            }
            else
            {
                //((Item)GDOUtils.GetExistingGDO(ItemReferences.SharpKnife)).Properties.Add(new CKillsCustomer());
                ((Item)GDOUtils.GetExistingGDO(ItemReferences.Mince)).DerivedProcesses.Add(new Item.ItemProcess()
                {
                    Process = (Process)GDOUtils.GetExistingGDO(ProcessReferences.Knead),
                    Result = (Item)GDOUtils.GetExistingGDO(ItemReferences.BurgerPattyRaw),
                    Duration = 0.75f
                });

                SetupSFX(args.gamedata);
            }
        }
        
        /// <summary>
        /// Loads the stab, poison, and alert audio assets into the game's referable clip registry.
        /// </summary>
        /// <param name="gameData">The game data collection that receives the mod-specific sound effects.</param>
        private void SetupSFX(GameData gameData)
        {
            // Guard: ensure the asset bundle is available before attempting to resolve audio clips.
            if (Bundle == null)
            {
                DebugLogSystem.LogWarning("Mystery Meat skipped SFX setup because the asset bundle is unavailable.");
                return;
            }

            #region Stab
            StabSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-STAB");

            // Guard: ensure the stab clip slot exists before injecting the audio assets.
            if (!gameData.ReferableObjects.Clips.ContainsKey(StabSoundEvent))
            {
                gameData.ReferableObjects.Clips.Add(StabSoundEvent, new AudioAssetRandom());
            }

            List<AudioClip> stabClips = new List<AudioClip>();

            // Guard: load each stab clip when available and report missing assets for diagnostics.
            LoadClipIntoCollection("stab-01", stabClips);
            LoadClipIntoCollection("stab-02", stabClips);
            LoadClipIntoCollection("stab-03", stabClips);

            typeof(AudioAssetRandom)
                .GetField("Clips", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(gameData.ReferableObjects.Clips[StabSoundEvent], stabClips);
            #endregion

            #region Poison
            PoisonSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-POISON");

            // Guard: ensure the poison clip slot exists before injecting the audio asset.
            if (!gameData.ReferableObjects.Clips.ContainsKey(PoisonSoundEvent))
            {
                gameData.ReferableObjects.Clips.Add(PoisonSoundEvent, new AudioAsset());
            }

            AudioClip poison1 = LoadClip("blub");

            // Guard: only inject the poison clip when the asset has been resolved successfully.
            if (poison1 != null)
            {
                typeof(AudioAsset)
                    .GetField("Clip", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(gameData.ReferableObjects.Clips[PoisonSoundEvent], poison1);
            }
            #endregion

            #region Alert
            AlertSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-ALERT");

            // Guard: ensure the alert clip slot exists before injecting the audio asset.
            if (!gameData.ReferableObjects.Clips.ContainsKey(AlertSoundEvent))
            {
                gameData.ReferableObjects.Clips.Add(AlertSoundEvent, new AudioAsset());
            }

            AudioClip alert1 = LoadClip("alert");

            // Guard: only inject the alert clip when the asset has been resolved successfully.
            if (alert1 != null)
            {
                typeof(AudioAsset)
                    .GetField("Clip", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(gameData.ReferableObjects.Clips[AlertSoundEvent], alert1);
            }
            #endregion
        }

        /// <summary>
        /// Loads an audio clip from the asset bundle and appends it to the provided collection when available.
        /// </summary>
        /// <param name="assetName">The name of the audio asset to load.</param>
        /// <param name="clips">The collection receiving the loaded clip.</param>
        private void LoadClipIntoCollection(string assetName, List<AudioClip> clips)
        {
            AudioClip clip = LoadClip(assetName);

            // Guard: only append clips that were resolved successfully.
            if (clip != null)
            {
                clips.Add(clip);
            }
        }

        /// <summary>
        /// Loads an audio clip from the asset bundle while logging when the asset cannot be found.
        /// </summary>
        /// <param name="assetName">The name of the audio asset to load.</param>
        /// <returns>The loaded audio clip when available; otherwise, null.</returns>
        private AudioClip LoadClip(string assetName)
        {
            // Guard: ensure the asset bundle exists before resolving the clip.
            if (Bundle == null)
            {
                DebugLogSystem.LogWarning($"Mystery Meat attempted to load audio clip '{assetName}' without an asset bundle.");
                return null;
            }

            AudioClip clip = Bundle.LoadAsset<AudioClip>(assetName);

            // Guard: report missing clips to aid in diagnosing asset regressions.
            if (clip == null)
            {
                DebugLogSystem.LogWarning($"Mystery Meat could not locate audio clip '{assetName}' in the asset bundle.");
            }
            else
            {
                clip.LoadAudioData();
            }

            return clip;
        }

        /// <summary>
        /// Attempts to resolve the activation context exposed by BaseMod using reflection for initialisation tasks.
        /// </summary>
        /// <returns>The activation context when available; otherwise, null.</returns>
        private KitchenMods.Mod ResolveActivationContext()
        {
            KitchenMods.Mod activationContext = null;

            // Guard: attempt to retrieve the activation context from the BaseMod implementation using reflection.
            if (_activationContextProperty == null)
            {
                _activationContextProperty = typeof(BaseMod).GetProperty("Mod", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }

            if (_activationContextProperty != null)
            {
                try
                {
                    activationContext = _activationContextProperty.GetValue(this) as KitchenMods.Mod;
                }
                catch (TargetException)
                {
                    DebugLogSystem.LogVerbose("Mystery Meat could not access the activation context property via reflection.");
                }
            }

            // Guard: fall back to the cached activation context when reflection does not expose it.
            if (activationContext == null)
            {
                activationContext = _cachedActivationContext;
            }

            // Guard: log verbose details only once when the context cannot be resolved during initialisation.
            if (activationContext == null && !_activationContextWarningLogged)
            {
                DebugLogSystem.LogVerbose("Mystery Meat could not resolve the activation context during OnInitialise; asset loading will retry after activation.");
                _activationContextWarningLogged = true;
            }

            return activationContext;
        }

        /// <summary>
        /// Caches the activation context for scenarios where OnInitialise executes before BaseMod exposes the instance.
        /// </summary>
        /// <param name="mod">The activation context supplied by KitchenLib.</param>
        private void CacheActivationContext(KitchenMods.Mod mod)
        {
            // Guard: ignore caching when the activation context has not been supplied.
            if (mod == null)
            {
                return;
            }

            _cachedActivationContext = mod;
        }

        /// <summary>
        /// Stores the reflection metadata used to resolve the BaseMod activation context.
        /// </summary>
        private static PropertyInfo _activationContextProperty;

        /// <summary>
        /// Tracks whether the activation context warning has been logged to avoid repeated spam.
        /// </summary>
        private static bool _activationContextWarningLogged;

        /// <summary>
        /// Caches the activation context provided during activation for initialisation retries.
        /// </summary>
        private static KitchenMods.Mod _cachedActivationContext;
    }
}
