using System;
using KitchenLib;
using KitchenLib.Logging.Exceptions;
using KitchenMods;
using System.Collections;
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

        private static KitchenMods.Mod _modContext;
        private static bool _assetsInitialised;
        private static bool _preferencesInitialised;
        private static bool _buildGameDataEventSubscribed;
        private static bool _runtimeHooksRegistered;

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

        public Mod() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, ModVersionString, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

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
        /// Handles initial mod setup, including logger configuration, asset acquisition, and preference preparation.
        /// </summary>
        protected override void OnInitialise()
        {
            // Ensure the logger exists before any other system attempts to emit diagnostic output.
            Logger = InitLogger();

            // Initialise the debug helper immediately so all subsequent log calls share the same configuration.
            DebugLogSystem.Initialise(Logger, () => ActiveDebugLogLevel);
            DebugLogSystem.LogVerbose("Logger initialised during OnInitialise.");

            // Attempt to resolve the mod context prior to loading assets so the bundle is available early.
            bool initialisedDuringLoad = EnsureInitialisation(null);

            // Guard: communicate when dependencies are unavailable so activation can complete the workflow later.
            if (!initialisedDuringLoad)
            {
                DebugLogSystem.LogWarning("Initialisation deferred until activation because the asset bundle or preferences were not fully ready during OnInitialise.");
            }
            else
            {
                CompleteRuntimeRegistration();
                DebugLogSystem.LogVerbose("Initialisation completed successfully during OnInitialise.");
            }
        }

        /// <summary>
        /// Handles per-frame updates for the mod.
        /// </summary>
        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// Handles final activation tasks such as completing any deferred setup, registering game data, and emitting the banner.
        /// </summary>
        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            // Ensure the initialisation routine completes even if portions were deferred during OnInitialise.
            bool initialised = EnsureInitialisation(mod);

            // Guard: skip runtime registrations when initialisation failed to protect against null bundles or preferences.
            if (!initialised)
            {
                DebugLogSystem.LogError("Mystery Meat activation aborted because required assets or preferences were unavailable during OnPostActivate.");
            }
            else
            {
                CompleteRuntimeRegistration();
                DebugLogSystem.LogInfo(ModLoadedBanner);
                DebugLogSystem.LogVerbose("Mystery Meat activation completed.");
            }
        }

        /// <summary>
        /// Finalises runtime hooks once assets and preferences are available.
        /// </summary>
        private void CompleteRuntimeRegistration()
        {
            bool prerequisitesReady = _assetsInitialised && _preferencesInitialised && Bundle != null && PrefManager != null;

            // Guard: ensure assets and preferences exist before wiring runtime hooks.
            if (!prerequisitesReady)
            {
                DebugLogSystem.LogError("Mystery Meat runtime registration skipped because assets or preferences were unavailable.");
            }
            else if (_runtimeHooksRegistered)
            {
                DebugLogSystem.LogVerbose("Mystery Meat runtime registration was already completed; skipping duplicate work.");
            }
            else
            {
                RegisterConfiguredCards();
                SubscribeToBuildGameDataEvent();
                _runtimeHooksRegistered = true;
                DebugLogSystem.LogVerbose("Mystery Meat runtime hooks registered successfully.");
            }
        }

        /// <summary>
        /// Registers gameplay cards based on the active preference configuration.
        /// </summary>
        private void RegisterConfiguredCards()
        {
            // Guard: ensure the preference manager is available before interrogating configuration values.
            if (PrefManager == null)
            {
                DebugLogSystem.LogError("Mystery Meat preferences were unavailable during activation; gameplay cards were not registered to avoid inconsistent state.");
            }
            else
            {
                // Collects the card registration actions alongside their controlling preference identifiers.
                var cardRegistrations = new (string PreferenceId, Action Register, string CardName)[]
                {
                    (CAUTIOUS_CROWD_ENABLED_ID, () => AddGameDataObject<CautiousCrowdCard>(), nameof(CautiousCrowdCard)),
                    (MESSY_MURDER_ENABLED_ID, () => AddGameDataObject<MessyMurderCard>(), nameof(MessyMurderCard)),
                    (PERSISTENT_CORPSES_ENABLED_ID, () => AddGameDataObject<PersistentCorpsesCard>(), nameof(PersistentCorpsesCard))
                };

                foreach (var card in cardRegistrations)
                {
                    // Guard: evaluate the stored preference before registering the associated game data object.
                    bool isEnabled = PrefManager.Get<bool>(card.PreferenceId);

                    if (isEnabled)
                    {
                        card.Register();
                        DebugLogSystem.LogVerbose($"Registered {card.CardName} because its preference is enabled.");
                    }
                    else
                    {
                        DebugLogSystem.LogVerbose($"Skipped registration for {card.CardName} because its preference is disabled.");
                    }
                }
            }
        }

        /// <summary>
        /// Subscribes to the build game data event when assets and preferences are ready for runtime hooks.
        /// </summary>
        private void SubscribeToBuildGameDataEvent()
        {
            // Guard: prevent duplicate subscriptions when activation occurs more than once.
            if (_buildGameDataEventSubscribed)
            {
                DebugLogSystem.LogVerbose("Mystery Meat BuildGameDataEvent subscription was already active; skipping duplicate registration.");
            }
            else
            {
                // Determines whether runtime prerequisites have been satisfied before subscribing.
                bool runtimeReady = _assetsInitialised && _preferencesInitialised && Bundle != null && PrefManager != null;

                // Guard: ensure the runtime dependencies exist before wiring the build game data event.
                if (!runtimeReady)
                {
                    DebugLogSystem.LogError("Mystery Meat skipped BuildGameDataEvent subscription because assets or preferences were not initialised.");
                }
                else
                {
                    Events.BuildGameDataEvent += OnBuildGameData;
                    _buildGameDataEventSubscribed = true;
                    DebugLogSystem.LogVerbose("Mystery Meat subscribed to BuildGameDataEvent.");
                }
            }
        }

        /// <summary>
        /// Handles the BuildGameData event by extending mince behaviour and registering custom audio clips.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event data containing the game database.</param>
        private void OnBuildGameData(object sender, BuildGameDataEventArgs args)
        {
            // Guard: verify that the event supplied valid game data before applying modifications.
            bool hasGameData = args?.gamedata != null;

            if (!hasGameData)
            {
                DebugLogSystem.LogWarning("Mystery Meat received BuildGameDataEvent without valid game data; mince extensions and SFX registration were skipped.");
            }
            else if (Bundle == null)
            {
                DebugLogSystem.LogError("Mystery Meat BuildGameDataEvent handler skipped because the asset bundle was unavailable.");
            }
            else
            {
                // Extends mince with a kneading process to produce burger patties as soon as game data is available.
                Item mince = (Item)GDOUtils.GetExistingGDO(ItemReferences.Mince);
                Process knead = (Process)GDOUtils.GetExistingGDO(ProcessReferences.Knead);
                Item patty = (Item)GDOUtils.GetExistingGDO(ItemReferences.BurgerPattyRaw);

                bool hasProcess = mince.DerivedProcesses.Any(p => p.Process == knead && p.Result == patty);

                // Guard: avoid duplicating the derived process when the handler executes more than once.
                if (!hasProcess)
                {
                    mince.DerivedProcesses.Add(new Item.ItemProcess()
                    {
                        Process = knead,
                        Result = patty,
                        Duration = 0.75f
                    });
                    DebugLogSystem.LogVerbose("Mystery Meat added knead-to-patty process to mince during BuildGameDataEvent.");
                }

                // Ensures the custom stab, poison, and alert audio clips are registered for runtime use.
                SetupSFX(args.gamedata);
            }
        }

        /// <summary>
        /// Loads the stab, poison, and alert audio assets into the game's referable clip registry.
        /// </summary>
        /// <param name="gameData">The game data collection that receives the mod-specific sound effects.</param>
        private void SetupSFX(GameData gameData)
        {
            bool dependenciesReady = gameData != null && Bundle != null;

            // Guard: ensure dependencies are ready before touching game data or bundle assets.
            if (!dependenciesReady)
            {
                // Guard: surface helpful diagnostics when the event did not provide game data.
                if (gameData == null)
                {
                    DebugLogSystem.LogWarning("Mystery Meat skipped SFX registration because BuildGameDataEvent supplied null game data.");
                }

                // Guard: emit an error when the bundle is missing because no audio clips can be registered.
                if (Bundle == null)
                {
                    DebugLogSystem.LogError("Mystery Meat skipped SFX registration because the asset bundle was unavailable.");
                }
            }
            else
            {
                #region Stab
                StabSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-STAB");

                // Guard: register the stab sound container when the clip registry does not yet know the event.
                if (!gameData.ReferableObjects.Clips.ContainsKey(StabSoundEvent))
                {
                    gameData.ReferableObjects.Clips.Add(StabSoundEvent, new AudioAssetRandom());
                }

                var stab1 = Bundle.LoadAsset<AudioClip>("stab-01");
                stab1.LoadAudioData();
                var stab2 = Bundle.LoadAsset<AudioClip>("stab-02");
                stab2.LoadAudioData();
                var stab3 = Bundle.LoadAsset<AudioClip>("stab-03");
                stab3.LoadAudioData();

                typeof(AudioAssetRandom)
                    .GetField("Clips", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(gameData.ReferableObjects.Clips[StabSoundEvent], new List<AudioClip>() { stab1, stab2, stab3 });
                #endregion

                #region Poison
                PoisonSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-POISON");

                // Guard: register the poison sound container when the clip registry does not yet know the event.
                if (!gameData.ReferableObjects.Clips.ContainsKey(PoisonSoundEvent))
                {
                    gameData.ReferableObjects.Clips.Add(PoisonSoundEvent, new AudioAsset());
                }

                var poison1 = Bundle.LoadAsset<AudioClip>("blub");
                poison1.LoadAudioData();

                typeof(AudioAsset)
                    .GetField("Clip", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(gameData.ReferableObjects.Clips[PoisonSoundEvent], poison1);
                #endregion

                #region Alert
                AlertSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-ALERT");

                // Guard: register the alert sound container when the clip registry does not yet know the event.
                if (!gameData.ReferableObjects.Clips.ContainsKey(AlertSoundEvent))
                {
                    gameData.ReferableObjects.Clips.Add(AlertSoundEvent, new AudioAsset());
                }

                var alert1 = Bundle.LoadAsset<AudioClip>("alert");
                alert1.LoadAudioData();

                typeof(AudioAsset)
                    .GetField("Clip", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(gameData.ReferableObjects.Clips[AlertSoundEvent], alert1);
                #endregion
            }
        }

        /// <summary>
        /// Ensures that assets and preferences are initialised, optionally using the provided mod reference.
        /// </summary>
        /// <param name="mod">The mod context supplied during activation, or null when invoked during OnInitialise.</param>
        /// <returns>True when all initialisation requirements have been satisfied.</returns>
        private bool EnsureInitialisation(KitchenMods.Mod mod)
        {
            KitchenMods.Mod resolvedMod = ResolveModContext(mod);

            InitialiseAssets(resolvedMod);
            InitialisePreferences();

            bool initialised = _assetsInitialised && _preferencesInitialised;
            return initialised;
        }

        /// <summary>
        /// Resolves the KitchenMods mod context either from activation or via the preloaded mod registry.
        /// </summary>
        /// <param name="mod">An optional mod reference obtained during activation.</param>
        /// <returns>The resolved mod context, or null when unavailable.</returns>
        private static KitchenMods.Mod ResolveModContext(KitchenMods.Mod mod)
        {
            KitchenMods.Mod resolvedContext = _modContext;

            // Guard: prefer the provided mod reference because it is authoritative during activation.
            if (mod != null)
            {
                _modContext = mod;
                resolvedContext = _modContext;
            }
            else if (resolvedContext == null)
            {
                try
                {
                    FieldInfo modsField = typeof(ModPreload).GetField("Mods", BindingFlags.Public | BindingFlags.Static);

                    // Guard: ensure the ModPreload dictionary is available before iterating over entries.
                    if (modsField?.GetValue(null) is IDictionary modsDictionary)
                    {
                        foreach (DictionaryEntry entry in modsDictionary)
                        {
                            // Guard: only capture entries that expose a KitchenMods.Mod instance for the Mystery Meat identifier.
                            if (entry.Value is KitchenMods.Mod candidate && string.Equals(candidate.ID, MOD_GUID, StringComparison.OrdinalIgnoreCase))
                            {
                                _modContext = candidate;
                                DebugLogSystem.LogVerbose("Resolved Mystery Meat mod context from ModPreload.");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogSystem.LogError($"Failed to resolve the Mystery Meat mod context: {ex.Message}");
                }

                resolvedContext = _modContext;
            }

            return resolvedContext;
        }

        /// <summary>
        /// Loads the Mystery Meat asset bundle and prepares TextMesh Pro resources when possible.
        /// </summary>
        /// <param name="mod">The mod reference providing access to the asset bundle packs.</param>
        private static void InitialiseAssets(KitchenMods.Mod mod)
        {
            bool loadSuccessful = Bundle != null;

            // Guard: only attempt to load assets when they have not been initialised yet.
            if (!loadSuccessful)
            {
                // Guard: defer loading when the mod context is unavailable during initialisation.
                if (mod == null)
                {
                    DebugLogSystem.LogWarning("Asset bundle loading deferred because the mod context was unavailable during initialisation.");
                }
                else
                {
                    try
                    {
                        Bundle = mod.GetPacks<AssetBundleModPack>().SelectMany(e => e.AssetBundles).FirstOrDefault() ?? throw new MissingAssetBundleException(MOD_GUID);

                        Bundle.LoadAllAssets<Texture2D>();
                        Bundle.LoadAllAssets<Sprite>();

                        TMP_SpriteAsset spriteAsset = Bundle.LoadAsset<TMP_SpriteAsset>("GrindMeat");

                        if (spriteAsset == null)
                        {
                            // Guard: ensure the sprite asset was located before configuring fallback settings.
                            DebugLogSystem.LogError("Failed to locate the GrindMeat sprite asset within the Mystery Meat bundle.");
                        }
                        else
                        {
                            TMP_Settings.defaultSpriteAsset.fallbackSpriteAssets.Add(spriteAsset);
                            spriteAsset.material = UnityEngine.Object.Instantiate(TMP_Settings.defaultSpriteAsset.material);

                            Texture2D spriteTexture = Bundle.LoadAsset<Texture2D>("GrindMeatTex");

                            // Guard: confirm the grind meat texture exists before applying it to the cloned material.
                            if (spriteTexture == null)
                            {
                                DebugLogSystem.LogError("Failed to locate the GrindMeat texture within the Mystery Meat bundle.");
                            }
                            else
                            {
                                spriteAsset.material.mainTexture = spriteTexture;
                                loadSuccessful = true;
                                DebugLogSystem.LogVerbose("Mystery Meat asset bundle initialised successfully.");
                            }
                        }
                    }
                    catch (MissingAssetBundleException)
                    {
                        DebugLogSystem.LogError("Mystery Meat asset bundle missing; the mod cannot continue without the required assets.");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        DebugLogSystem.LogError($"Unexpected error while initialising the Mystery Meat assets: {ex.Message}");
                        throw;
                    }
                }
            }

            _assetsInitialised = loadSuccessful;
        }

        /// <summary>
        /// Builds the Mystery Meat preference menus, including audio, card, and debugging options.
        /// </summary>
        private static void InitialisePreferences()
        {
            bool preferencesReady = PrefManager != null;

            // Guard: build the preference hierarchy only once to avoid duplicate registration.
            if (!preferencesReady)
            {
                PrefManager = new PreferenceSystemManager(MOD_GUID, MOD_NAME);

                IntArrayGenerator intArrayGenerator = new IntArrayGenerator();
                intArrayGenerator.AddRange(0, 100, 10, null, delegate (string prefKey, int value)
                {
                    return $"{value}%";
                });
                int[] zeroToHundredPercentValues = intArrayGenerator.GetArray();
                string[] zeroToHundredPercentStrings = intArrayGenerator.GetStrings();
                int[] debugLogLevelValues = new[]
                {
                    (int)DebugLogLevel.Off,
                    (int)DebugLogLevel.On,
                    (int)DebugLogLevel.Verbose
                };
                string[] debugLogLevelLabels = new[]
                {
                    "Off",
                    "On",
                    "Verbose"
                };
                intArrayGenerator.Clear();

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
                DebugLogSystem.LogVerbose("Mystery Meat preferences initialised successfully.");
                preferencesReady = true;
            }

            _preferencesInitialised = preferencesReady;
        }
    }
}
