using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Items;
using KitchenMysteryMeat.Systems.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Appliances
{
    public class PoisonProvider : CustomAppliance
    {
        public override string UniqueNameID => "PoisonProvider";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Poison Provider").AssignMaterialsByNames().AssignVFXByNames();
        public override PriceTier PriceTier => PriceTier.VeryExpensive;
        public override RarityTier RarityTier => RarityTier.Uncommon;
        public override bool SellOnlyAsDuplicate => true;
        public override bool IsPurchasable => true;
        public override ShoppingTags ShoppingTags => ShoppingTags.Misc;

        public override List<(Locale, ApplianceInfo)> InfoList => new()
        {
            ( Locale.English, LocalisationUtils.CreateApplianceInfo("Poison Provider", "Used for butchering", new()
            { new Appliance.Section
                {
                    Title = "Sharp",
                    Description = "Hold this to <sprite name=\"chop\" color=#A8FF1E> 3x faster!"
                }
            }, new()) )
        };

        public override List<Process> RequiresProcessForShop => new()
        {
        };

        public override List<IApplianceProperty> Properties => new()
        {
            new CItemHolder(),
            KitchenPropertiesUtils.GetCItemProvider(GDOUtils.GetCustomGameDataObject<PoisonBottle>().ID, 1, 1, false, false, true, false, false, true, false),
        };

        /// <summary>
        /// Configures the countertop to dispense poison bottles for gameplay interactions.
        /// </summary>
        public override void OnRegister(Appliance gameDataObject)
        {
            Helper.SetupCounterLimitedItem(Prefab, Mod.Bundle.LoadAsset<GameObject>("Poison Bottle").AssignMaterialsByNames().AssignVFXByNames());
            DebugLogSystem.LogVerbose("Initialised limited item setup during registration.");
        }
    }
}
