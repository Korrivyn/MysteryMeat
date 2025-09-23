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
        private static bool _buildGameDataSubscribed;

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
        /// Handles initial mod setup by preparing core systems, registering cards, and subscribing to runtime hooks.
        /// </summary>
        protected override void OnInitialise()
        {
            // Prepare logging and preferences so subsequent operations can query configuration safely.
            bool coreReady = EnsureCoreInitialisation();

            // Guard: abort further setup when the logger or preferences are unavailable.
            if (!coreReady)
            {
                DebugLogSystem.LogError("Mystery Meat failed to initialise its core systems; runtime registrations have been skipped to avoid inconsistent state.");
            }
            else
            {
                // Register enabled cards during initialisation so gameplay reflects the configured preferences immediately.
                RegisterEnabledCards();

                // Subscribe to BuildGameData once so later activation phases can safely supply assets.
                SubscribeToBuildGameDataEvent();
            }
        }

        /// <summary>
        /// Handles per-frame updates for the mod.
        /// </summary>
        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// Handles post-activation duties by ensuring assets are available and displaying the mod banner.
        /// </summary>
        /// <param name="mod">The activation context supplied by KitchenLib.</param>
        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            // Load the asset bundle using the activation context so runtime systems have access to shared resources.
            bool assetsReady = EnsureAssetBundle(mod);

            // Guard: display the banner only when assets are ready to prevent misleading confirmation messages.
            if (assetsReady)
            {
                DebugLogSystem.LogInfo(ModLoadedBanner);

                // Ensure event subscriptions occur once activation has provided the required assets.
                SubscribeToBuildGameDataEvent();
            }
        }

        /// <summary>
        /// Ensures the mod logger, preferences, and assets are prepared for runtime use.
        /// </summary>
        /// <param name="mod">The activation context that exposes asset bundles when available.</param>
        /// <returns>True when the assets and preferences required for runtime hooks are ready.</returns>
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

            // Guard: construct the preference manager exactly once so menu registration is idempotent.
            if (PrefManager == null)
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
                    AssetBundle resolvedBundle = mod
                        .GetPacks<AssetBundleModPack>()
                        .SelectMany(pack => pack.AssetBundles)
                        .FirstOrDefault();

                    // Guard: confirm the asset bundle has been found before attempting to cache it.
                    if (resolvedBundle != null)
                    {
                        Bundle = resolvedBundle;

                        // Preload commonly used assets so runtime lookups occur without additional I/O.
                        Bundle.LoadAllAssets<Texture2D>();
                        Bundle.LoadAllAssets<Sprite>();

                        TMP_SpriteAsset spriteAsset = Bundle.LoadAsset<TMP_SpriteAsset>("GrindMeat");

                        // Guard: register the sprite asset as a fallback only once to avoid duplicate references.
                        if (spriteAsset != null && !TMP_Settings.defaultSpriteAsset.fallbackSpriteAssets.Contains(spriteAsset))
                        {
                            TMP_Settings.defaultSpriteAsset.fallbackSpriteAssets.Add(spriteAsset);
                        }

                        // Guard: configure the sprite asset only when it has been loaded successfully.
                        if (spriteAsset != null)
                        {
                            spriteAsset.material = UnityEngine.Object.Instantiate(TMP_Settings.defaultSpriteAsset.material);
                            spriteAsset.material.mainTexture = Bundle.LoadAsset<Texture2D>("GrindMeatTex");
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
            #region Stab
            StabSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-STAB");

            // Guard: ensure the stab clip slot exists before injecting the audio assets.
            if (!gameData.ReferableObjects.Clips.ContainsKey(StabSoundEvent))
            {
                gameData.ReferableObjects.Clips.Add(StabSoundEvent, new AudioAssetRandom());
            }

            AudioClip stab1 = Bundle.LoadAsset<AudioClip>("stab-01");
            stab1.LoadAudioData();
            AudioClip stab2 = Bundle.LoadAsset<AudioClip>("stab-02");
            stab2.LoadAudioData();
            AudioClip stab3 = Bundle.LoadAsset<AudioClip>("stab-03");
            stab3.LoadAudioData();

            typeof(AudioAssetRandom)
                .GetField("Clips", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(gameData.ReferableObjects.Clips[StabSoundEvent], new List<AudioClip>() { stab1, stab2, stab3 });
            #endregion

            #region Poison
            PoisonSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-POISON");

            // Guard: ensure the poison clip slot exists before injecting the audio asset.
            if (!gameData.ReferableObjects.Clips.ContainsKey(PoisonSoundEvent))
            {
                gameData.ReferableObjects.Clips.Add(PoisonSoundEvent, new AudioAsset());
            }

            AudioClip poison1 = Bundle.LoadAsset<AudioClip>("blub");
            poison1.LoadAudioData();

            typeof(AudioAsset)
                .GetField("Clip", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(gameData.ReferableObjects.Clips[PoisonSoundEvent], poison1);
            #endregion

            #region Alert
            AlertSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-ALERT");

            // Guard: ensure the alert clip slot exists before injecting the audio asset.
            if (!gameData.ReferableObjects.Clips.ContainsKey(AlertSoundEvent))
            {
                gameData.ReferableObjects.Clips.Add(AlertSoundEvent, new AudioAsset());
            }

            AudioClip alert1 = Bundle.LoadAsset<AudioClip>("alert");
            alert1.LoadAudioData();

            typeof(AudioAsset)
                .GetField("Clip", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(gameData.ReferableObjects.Clips[AlertSoundEvent], alert1);
            #endregion
        }
    }
}
