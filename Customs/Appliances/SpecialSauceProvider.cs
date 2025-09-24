using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
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
    public class SpecialSauceProvider : CustomAppliance
    {
        public override string UniqueNameID => "SpecialSauceProvider";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Special Sauce Provider").AssignMaterialsByNames().AssignVFXByNames();
        public override PriceTier PriceTier => PriceTier.Expensive;
        public override RarityTier RarityTier => RarityTier.Uncommon;
        public override bool SellOnlyAsDuplicate => true;
        public override bool IsPurchasable => true;
        public override ShoppingTags ShoppingTags => ShoppingTags.Misc;

        public override List<(Locale, ApplianceInfo)> InfoList => new()
        {
            ( Locale.English, LocalisationUtils.CreateApplianceInfo("Special Sauce", "Requires a top-secret ingredient!", new(), new()) )
        };

        public override List<Process> RequiresProcessForShop => new()
        {
        };
        public override List<Appliance.ApplianceProcesses> Processes => new List<Appliance.ApplianceProcesses>()
        {
            new Appliance.ApplianceProcesses()
            {
                Process = GDOUtils.GetExistingGDO(ProcessReferences.Chop) as Process,
                Speed = 0.75f,
                IsAutomatic = false,
                Validity = ProcessValidity.Generic
            },
            new Appliance.ApplianceProcesses()
            {
                Process = GDOUtils.GetExistingGDO(ProcessReferences.Knead) as Process,
                Speed = 0.75f,
                IsAutomatic = false,
                Validity = ProcessValidity.Generic
            },
        };

        public override List<IApplianceProperty> Properties => new()
        {
            new CItemHolder(),
            KitchenPropertiesUtils.GetCItemProvider(GDOUtils.GetCustomGameDataObject<EmptySpecialSauceBottle>().ID, 1, 1, false, false, true, false, false, true, false),
        };

        /// <summary>
        /// Configures the countertop prefab to dispense special sauce bottles.
        /// </summary>
        public override void OnRegister(Appliance gameDataObject)
        {
            Helper.SetupCounterLimitedItem(Prefab, Mod.Bundle.LoadAsset<GameObject>("Empty Special Sauce Bottle").AssignMaterialsByNames().AssignVFXByNames());
            DebugLogSystem.LogVerbose("SpecialSauceProvider initialised limited item setup during registration.");
        }
    }
}
