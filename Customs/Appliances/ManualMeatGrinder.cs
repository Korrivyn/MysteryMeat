using Kitchen;
using Kitchen.Components;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Processes;
using KitchenMysteryMeat.MonoBehaviours;
using KitchenMysteryMeat.Views;
using KitchenMysteryMeat.Systems.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Appliances
{
    public class ManualMeatGrinder : CustomAppliance
    {
        public override string UniqueNameID => "ManualMeatGrinder";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Manual Meat Grinder").AssignMaterialsByNames();
        public override PriceTier PriceTier => PriceTier.Medium;
        public override bool SellOnlyAsDuplicate => true;
        public override bool IsPurchasable => true;
        public override ShoppingTags ShoppingTags => ShoppingTags.Cooking | ShoppingTags.Misc;
        public override List<Appliance> Upgrades => new List<Appliance>()
        {
            GDOUtils.GetCastedGDO<Appliance, AutomaticMeatGrinder>()
        };
        public override List<(Locale, ApplianceInfo)> InfoList => new()
        {
            ( Locale.English, LocalisationUtils.CreateApplianceInfo("Manual Meat Grinder", "Use this to make mincemeat of someone. Literally.", new(), new()) )
        };

        public override List<IApplianceProperty> Properties => new()
        {
            new CItemHolder(),
            /*new CItemTransferRestrictions()
            {
                AllowWhenActive = false,
                AllowWhenInactive = true
            }*/
            new CMeatGrinder()
            {
                GrindProcess = GDOUtils.GetCastedGDO<Process, GrindMeat>().ID,
                GrinderInputPosition = new Vector3(0, 1f, 0.2f),
                GrinderOutputPosition = new Vector3(0, 0.5f, -0.3f),
            },
        };


        public override List<Appliance.ApplianceProcesses> Processes => new List<Appliance.ApplianceProcesses>()
        {
            new Appliance.ApplianceProcesses()
            {
                Process = GDOUtils.GetCastedGDO<Process, GrindMeat>(),   // reference to the base process
                Speed = 0.75f,                                              // the speed multiplier when using this appliance (for reference, starter = 0.75, base = 1, danger hob/oven = 2)
                IsAutomatic = false                                       // (optional) whether the process is automatic on this appliance
            }
        };

        // Animator code courtesy of IcedMilo: https://github.com/UrFriendKen/PlateUpAutomationPlus/blob/master/Customs/SmartRotatingGrabber.cs
        static FieldInfo animator = typeof(ApplianceProcessView).GetField("Animator", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo clip = typeof(ApplianceProcessView).GetField("Clip", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo sound = typeof(ApplianceProcessView).GetField("Sound", BindingFlags.NonPublic | BindingFlags.Instance);

        static FieldInfo pushObject = typeof(ConveyItemsView).GetField("PushObject", BindingFlags.NonPublic | BindingFlags.Instance);
        /*static FieldInfo smartActive = typeof(ConveyItemsView).GetField("SmartActive", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo smartInactive = typeof(ConveyItemsView).GetField("SmartInactive", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo typeContainer = typeof(ConveyItemsView).GetField("TypeContainer", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo animator = typeof(ConveyItemsView).GetField("Animator", BindingFlags.NonPublic | BindingFlags.Instance);*/

        /// <summary>
        /// Configures hold points, audio, and view components when the manual grinder is registered.
        /// </summary>
        public override void OnRegister(Appliance gameDataObject)
        {
            base.OnRegister(gameDataObject);

            Prefab.AddComponent<HoldPointContainer>().HoldPoint = GameObjectUtils.GetChildObject(gameDataObject.Prefab, "GameObject/HoldPoint").transform;

            ApplianceProcessView applianceProcessView = Prefab.AddComponent<ApplianceProcessView>();

            animator.SetValue(applianceProcessView, Prefab.GetComponent<Animator>());

            PreferenceVolumeAdjuster volumeAdjuster = applianceProcessView.gameObject.AddComponent<PreferenceVolumeAdjuster>();
            AudioClip audioClip = Mod.Bundle.LoadAsset<AudioClip>("grinder.ogg");
            volumeAdjuster.PreferenceID = Mod.MEAT_GRINDER_VOLUME_ID;

            clip.SetValue(applianceProcessView, audioClip);


            ConveyItemsView conveyItemsView = gameDataObject.Prefab.AddComponent<ConveyItemsView>();
            pushObject.SetValue(conveyItemsView, GameObjectUtils.GetChildObject(gameDataObject.Prefab, "GameObject/HoldPoint"));


            MeatGrinderView meatGrinderView = gameDataObject.Prefab.AddComponent<MeatGrinderView>();
            DebugLogSystem.LogVerbose("Configured hold point, audio, and view components during registration.");
        }
    }
}
