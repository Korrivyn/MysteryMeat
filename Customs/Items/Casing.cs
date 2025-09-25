using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using KitchenMysteryMeat.Customs.Appliances;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Items
{
    /// <summary>
    /// Represents the casing used when assembling mystery meat hotdogs.
    /// </summary>
    public class Casing : CustomItem
    {
        public override string UniqueNameID => "Casing";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Casing").AssignMaterialsByNames();
        public override Appliance DedicatedProvider => (Appliance)GDOUtils.GetCustomGameDataObject<CasingsProvider>().GameDataObject;
        public override ItemStorage ItemStorageFlags => ItemStorage.StackableFood;

        /// <summary>
        /// Logs the provider relationship for hotdog casings upon registration.
        /// </summary>
        /// <param name="gameDataObject">The item definition being registered.</param>
        public override void OnRegister(Item gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int providerId = GDOUtils.GetCustomGameDataObject<CasingsProvider>().ID;

            DebugLogSystem.LogVerbose(
                $"Casing item registered with dedicated provider {providerId} and stackable storage flag.");
        }
    }
}
