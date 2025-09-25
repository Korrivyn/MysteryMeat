using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using KitchenMysteryMeat.Customs.Items;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Appliances
{
    /// <summary>
    /// Provides an unlimited supply of hotdog casings once the restaurant owns a meat cleaver provider.
    /// </summary>
    public class CasingsProvider : CustomAppliance
    {
        public override string UniqueNameID => "CasingsProvider";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Casings Provider").AssignMaterialsByNames().AssignVFXByNames();
        public override PriceTier PriceTier => PriceTier.VeryExpensive;
        public override RarityTier RarityTier => RarityTier.Uncommon;
        public override bool SellOnlyAsDuplicate => true;
        public override bool IsPurchasable => true;
        public override ShoppingTags ShoppingTags => ShoppingTags.Misc;

        public override List<(Locale, ApplianceInfo)> InfoList => new()
        {
            (Locale.English, LocalisationUtils.CreateApplianceInfo("Casings", "Provides hotdog casings", new(), new()))
        };

        public override List<IApplianceProperty> Properties => new()
        {
            KitchenPropertiesUtils.GetUnlimitedCItemProvider(GDOUtils.GetCustomGameDataObject<Casing>().ID)
        };

        public override List<Appliance> RequiresForShop => new()
        {
            (Appliance)GDOUtils.GetCustomGameDataObject<MeatCleaverProvider>().GameDataObject
        };

        /// <summary>
        /// Reports the provider requirements and unlimited stock configuration during registration.
        /// </summary>
        /// <param name="gameDataObject">The appliance definition being registered.</param>
        public override void OnRegister(Appliance gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int casingId = GDOUtils.GetCustomGameDataObject<Casing>().ID;
            int prerequisiteApplianceId = GDOUtils.GetCustomGameDataObject<MeatCleaverProvider>().ID;

            DebugLogSystem.LogVerbose(
                $"CasingsProvider registered unlimited casing supply (item {casingId}) with prerequisite appliance {prerequisiteApplianceId}.");
        }
    }
}
