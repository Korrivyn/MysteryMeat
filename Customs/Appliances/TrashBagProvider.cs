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
    /// Provides unlimited trash bags once the restaurant has invested in the meat cleaver provider.
    /// </summary>
    public class TrashBagProvider : CustomAppliance
    {
        public override string UniqueNameID => "TrashBagProvider";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Trash Bag Provider").AssignMaterialsByNames().AssignVFXByNames();
        public override PriceTier PriceTier => PriceTier.VeryExpensive;
        public override RarityTier RarityTier => RarityTier.Uncommon;
        public override bool SellOnlyAsDuplicate => true;
        public override bool IsPurchasable => true;
        public override ShoppingTags ShoppingTags => ShoppingTags.Misc;

        public override List<(Locale, ApplianceInfo)> InfoList => new()
        {
            (Locale.English, LocalisationUtils.CreateApplianceInfo("Trash Bags", "Large enough to fit some body!", new(), new()))
        };

        public override List<IApplianceProperty> Properties => new()
        {
            KitchenPropertiesUtils.GetUnlimitedCItemProvider(GDOUtils.GetCustomGameDataObject<TrashBag>().ID)
        };

        public override List<Appliance> RequiresForShop => new()
        {
            (Appliance)GDOUtils.GetCustomGameDataObject<MeatCleaverProvider>().GameDataObject
        };

        /// <summary>
        /// Emits verbose diagnostics about the trash bag stock and dependency requirements.
        /// </summary>
        /// <param name="gameDataObject">The appliance definition being registered.</param>
        public override void OnRegister(Appliance gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int trashBagId = GDOUtils.GetCustomGameDataObject<TrashBag>().ID;
            int prerequisiteApplianceId = GDOUtils.GetCustomGameDataObject<MeatCleaverProvider>().ID;

            DebugLogSystem.LogVerbose(
                $"TrashBagProvider registered unlimited trash bag supply (item {trashBagId}) with prerequisite appliance {prerequisiteApplianceId}.");
        }
    }
}
