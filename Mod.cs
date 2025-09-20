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

namespace KitchenMysteryMeat
{
    public class Mod : BaseMod, IModSystem, IAutoRegisterAll
    {
        public const string MOD_GUID = "com.quackandcheese.mysterymeat";
        public const string MOD_NAME = "Mystery Meat";
        public static new readonly Version ModVersion = new Version(0, 3, 0);
        public static string ModVersionString => ModVersion.ToString();
        public const string MOD_AUTHOR = "QuackAndCheese";
        public const string MOD_GAMEVERSION = ">=1.1.9";

        internal static AssetBundle Bundle;
        internal static KitchenLogger Logger;

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

        protected override void OnInitialise()
        {
            Logger.LogWarning($"{MOD_GUID} v{ModVersion} in use!");
        }

        protected override void OnUpdate()
        {
        }

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