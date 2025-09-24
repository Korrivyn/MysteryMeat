using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KitchenLib.References;
using KitchenMysteryMeat.Customs.Items;
using KitchenMysteryMeat.Systems.Logging;

namespace KitchenMysteryMeat.Customs.Appliances
{
    public class MeatCleaverProvider : CustomAppliance
    {
        public override string UniqueNameID => "MeatCleaverProvider";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Meat Cleaver Provider").AssignMaterialsByNames().AssignVFXByNames();
        public override PriceTier PriceTier => PriceTier.VeryExpensive;
        public override RarityTier RarityTier => RarityTier.Uncommon;
        public override bool SellOnlyAsDuplicate => true;
        public override bool IsPurchasable => true;
        public override ShoppingTags ShoppingTags => ShoppingTags.Misc;

        public override List<(Locale, ApplianceInfo)> InfoList => new()
        {
            ( Locale.English, LocalisationUtils.CreateApplianceInfo("Meat Cleaver", "Used for butchering", new()
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
            KitchenPropertiesUtils.GetCItemProvider(GDOUtils.GetCustomGameDataObject<MeatCleaver>().ID, 1, 1, false, false, false, false, false, false, false)
        };

        /// <summary>
        /// Configures the thin counter to provide meat cleavers to players.
        /// </summary>
        public override void OnRegister(Appliance gameDataObject)
        {
            Helper.SetupThinCounterLimitedItem(Prefab, Mod.Bundle.LoadAsset<GameObject>("Meat Cleaver").AssignMaterialsByNames().AssignVFXByNames(), false);
            DebugLogSystem.LogVerbose("MeatCleaverProvider initialised limited item setup during registration.");
        }
    }
}
