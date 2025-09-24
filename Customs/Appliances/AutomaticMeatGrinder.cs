using Kitchen;
using Kitchen.Components;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Processes;
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
    public class AutomaticMeatGrinder : CustomAppliance
    {
        public override string UniqueNameID => "AutomaticMeatGrinder";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Automatic Meat Grinder").AssignMaterialsByNames();
        public override PriceTier PriceTier => PriceTier.Expensive;
        public override bool SellOnlyAsDuplicate => true;
        public override bool IsPurchasable => false;
        public override bool IsPurchasableAsUpgrade => true;
        public override ShoppingTags ShoppingTags => ShoppingTags.Cooking | ShoppingTags.Misc;
        public override List<(Locale, ApplianceInfo)> InfoList => new()
        {
            ( Locale.English, LocalisationUtils.CreateApplianceInfo("Automatic Meat Grinder", "Use this to make mincemeat of someone. Literally.", new(), new()) )
        };

        public override List<IApplianceProperty> Properties => new()
        {
            new CItemHolder(),
            /*new CItemTransferRestrictions()
            {
                AllowWhenActive = false,
                AllowWhenInactive = true
            }*/
            new CConveyPushItems()
            {
                Delay = 1f,
                Push = true,
                Grab = false,
                Reversed = true,
                IgnoreProcessingItems = true,
            },
            new CConveyCooldown(),
            new CMeatGrinder()
            {
                GrindProcess = GDOUtils.GetCastedGDO<Process, GrindMeat>().ID,
                GrinderInputPosition = new Vector3(0, 1.147f, -0.032f),
                GrinderOutputPosition = new Vector3(0, 0.5f, -0.128f),
            },
        };


        public override List<Appliance.ApplianceProcesses> Processes => new List<Appliance.ApplianceProcesses>()
        {
            new Appliance.ApplianceProcesses()
            {
                Process = GDOUtils.GetCastedGDO<Process, GrindMeat>(),   // reference to the base process
                Speed = 1f,                                              // the speed multiplier when using this appliance (for reference, starter = 0.75, base = 1, danger hob/oven = 2)
                IsAutomatic = true                                       // (optional) whether the process is automatic on this appliance
            }
        };

        // Animator code courtesy of IcedMilo: https://github.com/UrFriendKen/PlateUpAutomationPlus/blob/master/Customs/SmartRotatingGrabber.cs
        static FieldInfo animator = typeof(ApplianceProcessView).GetField("Animator", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo clip = typeof(ApplianceProcessView).GetField("Clip", BindingFlags.NonPublic | BindingFlags.Instance);

        static FieldInfo pushObject = typeof(ConveyItemsView).GetField("PushObject", BindingFlags.NonPublic | BindingFlags.Instance);
        /*static FieldInfo smartActive = typeof(ConveyItemsView).GetField("SmartActive", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo smartInactive = typeof(ConveyItemsView).GetField("SmartInactive", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo typeContainer = typeof(ConveyItemsView).GetField("TypeContainer", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo animator = typeof(ConveyItemsView).GetField("Animator", BindingFlags.NonPublic | BindingFlags.Instance);*/

        /// <summary>
        /// Configures automation visuals and audio when the automatic grinder is registered.
        /// </summary>
        public override void OnRegister(Appliance gameDataObject)
        {
            base.OnRegister(gameDataObject);

            Prefab.AddComponent<HoldPointContainer>().HoldPoint = GameObjectUtils.GetChildObject(gameDataObject.Prefab, "GameObject/HoldPoint").transform;

            ApplianceProcessView applianceProcessView = Prefab.AddComponent<ApplianceProcessView>();
            animator.SetValue(applianceProcessView, Prefab.GetComponent<Animator>());
            clip.SetValue(applianceProcessView, Mod.Bundle.LoadAsset<AudioClip>("grinder.ogg"));


            ConveyItemsView conveyItemsView = gameDataObject.Prefab.AddComponent<ConveyItemsView>();
            pushObject.SetValue(conveyItemsView, GameObjectUtils.GetChildObject(gameDataObject.Prefab, "GameObject/HoldPoint"));


            MeatGrinderView meatGrinderView = gameDataObject.Prefab.AddComponent<MeatGrinderView>();
            DebugLogSystem.LogVerbose("AutomaticMeatGrinder configured automated components during registration.");
        }
    }
}
