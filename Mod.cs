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
        /// Gets the ASCII art banner displayed when the mod is initialised.
        /// </summary>
        private static string ModLoadedBanner
        {
            get
            {
                string[] bannerLines =
                {
                    @"      __  __         _                  __  __          _   ",
                    @"     |  \/  |_  _ __| |_ ___ _ _ _  _  |  \/  |___ __ _| |_ ",
                    @"     | |\/| | || (_-<  _/ -_) '_| || | | |\/| / -_) _` |  _|",
                    @"     |_|  |_|\_, /__/\__\___|_|  \_, | |_|  |_\___\__,_|\__|",
                    @"             |__/                |__/                       ",
                    @"",
                    @"⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠀⠀⠀⠀",
                    @"⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⡶⠶⣦⣄⠀⠀⢀⣴⣿⣷⡄⠀⠀",
                    @"⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⣴⣿⣿⡇⡖⠂⠙⠗⣠⣾⣿⣿⣿⣥⣀⠀",
                    @"⠀⠀⠀⠀⠀⠀⠀⢀⣀⣠⣤⣶⣿⣿⣿⣿⣿⣇⢣⠀⣠⣾⣿⣿⣿⣿⣿⣿⣿⠇",
                    @"⠀⠀⠀⠀⠀⠀⢰⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡌⢧⠘⠿⠟⠛⣉⠉⠀⠀⠀⠀",
                    @"⠀⠀⠀⠀⠀⠀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣌⠳⣄⠀⠀⣿⡀⠀⠀⠀⠀",
                    @"⠀⠀⠀⠀⠀⠀⢸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣌⣉⣁⡿⠀⠀⠀⠀⠀",
                    @"⠀⢀⣤⣤⣤⣤⡀⢻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⠟⠛⠉⠀⠀⠀⠀⠀⠀",
                    @"⠀⢻⣿⣿⣿⣿⣷⡀⠹⣿⣿⣿⣿⣿⣿⣿⣿⠟⠋⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
                    @"⠀⠀⠉⢩⣿⣿⣿⠋⠀⠈⠻⢿⣿⣿⣿⠋⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
                    @"⠀⠀⠀⠸⣿⡿⠁⠀⠀⠀⠀⠀⠈⠉⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
                    $"        Mystery Meat v{ModVersion}"
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
        /// Handles initial mod setup and displays the load notification banner.
        /// </summary>
        protected override void OnInitialise()
        {
            Logger.LogWarning(ModLoadedBanner);
        }

        /// <summary>
        /// Handles per-frame updates for the mod.
        /// </summary>
        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// Handles asset loading and preference initialisation after the mod is activated.
        /// </summary>
        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            Bundle = mod.GetPacks<AssetBundleModPack>().SelectMany(e => e.AssetBundles).FirstOrDefault() ?? throw new MissingAssetBundleException(MOD_GUID);
            Logger = InitLogger();

            Bundle.LoadAllAssets<Texture2D>();
            Bundle.LoadAllAssets<Sprite>();
            var spriteAsset = Bundle.LoadAsset<TMP_SpriteAsset>("GrindMeat");
            TMP_Settings.defaultSpriteAsset.fallbackSpriteAssets.Add(spriteAsset);
            spriteAsset.material = UnityEngine.Object.Instantiate(TMP_Settings.defaultSpriteAsset.material);
            spriteAsset.material.mainTexture = Bundle.LoadAsset<Texture2D>("GrindMeatTex");

            #region Preferences
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
            #endregion

            if (Mod.PrefManager.Get<bool>(CAUTIOUS_CROWD_ENABLED_ID))
            {
                AddGameDataObject<CautiousCrowdCard>();
            }
            if (Mod.PrefManager.Get<bool>(MESSY_MURDER_ENABLED_ID))
            {
                AddGameDataObject<MessyMurderCard>();
            }
            if (Mod.PrefManager.Get<bool>(PERSISTENT_CORPSES_ENABLED_ID))
            {
                AddGameDataObject<PersistentCorpsesCard>();
            }


            Events.BuildGameDataEvent += delegate (object s, BuildGameDataEventArgs args)
            {
                //((Item)GDOUtils.GetExistingGDO(ItemReferences.SharpKnife)).Properties.Add(new CKillsCustomer());
                ((Item)GDOUtils.GetExistingGDO(ItemReferences.Mince)).DerivedProcesses.Add(new Item.ItemProcess()
                {
                    Process = (Process)GDOUtils.GetExistingGDO(ProcessReferences.Knead),
                    Result = (Item)GDOUtils.GetExistingGDO(ItemReferences.BurgerPattyRaw),
                    Duration = 0.75f
                });

                SetupSFX(args.gamedata);
            };
        }
        
        private void SetupSFX(GameData gameData)
        {
            #region Stab
            StabSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-STAB");

            if (!gameData.ReferableObjects.Clips.ContainsKey(StabSoundEvent))
                gameData.ReferableObjects.Clips.Add(StabSoundEvent, new AudioAssetRandom());

            var stab1 = Bundle.LoadAsset<AudioClip>("stab-01"); stab1.LoadAudioData();
            var stab2 = Bundle.LoadAsset<AudioClip>("stab-02"); stab2.LoadAudioData();
            var stab3 = Bundle.LoadAsset<AudioClip>("stab-03"); stab3.LoadAudioData();

            typeof(AudioAssetRandom)
                .GetField("Clips", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(gameData.ReferableObjects.Clips[StabSoundEvent], new List<AudioClip>() { stab1, stab2, stab3 });
            #endregion

            #region Poison
            PoisonSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-POISON");

            if (!gameData.ReferableObjects.Clips.ContainsKey(PoisonSoundEvent))
                gameData.ReferableObjects.Clips.Add(PoisonSoundEvent, new AudioAsset());

            var poison1 = Bundle.LoadAsset<AudioClip>("blub"); poison1.LoadAudioData();

            typeof(AudioAsset)
                .GetField("Clip", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(gameData.ReferableObjects.Clips[PoisonSoundEvent], poison1);
            #endregion

            #region Alert
            AlertSoundEvent = (SoundEvent)VariousUtils.GetID(MOD_GUID + "-ALERT");

            if (!gameData.ReferableObjects.Clips.ContainsKey(AlertSoundEvent))
                gameData.ReferableObjects.Clips.Add(AlertSoundEvent, new AudioAsset());

            var alert1 = Bundle.LoadAsset<AudioClip>("alert"); alert1.LoadAudioData();

            typeof(AudioAsset)
                .GetField("Clip", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(gameData.ReferableObjects.Clips[AlertSoundEvent], alert1);
            #endregion
        }
    }
}
