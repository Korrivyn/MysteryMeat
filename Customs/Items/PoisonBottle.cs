using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Appliances;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Items
{
    /// <summary>
    /// Defines the poison bottle used to deliver lethal doses to customers.
    /// </summary>
    public class PoisonBottle : CustomItem
    {
        public override string UniqueNameID => "PoisonBottle";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Poison Bottle").AssignMaterialsByNames();
        public override Appliance DedicatedProvider => (Appliance)GDOUtils.GetCustomGameDataObject<PoisonProvider>().GameDataObject;
        public override bool IsIndisposable => true;
        public override ItemStorage ItemStorageFlags => ItemStorage.None;

        public override List<IItemProperty> Properties => new()
        {
            new CPoisonBottle()
        };

        /// <summary>
        /// Emits verbose diagnostics confirming poison bottle registration and provider linkage.
        /// </summary>
        /// <param name="gameDataObject">The item definition being registered.</param>
        public override void OnRegister(Item gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int poisonProviderId = GDOUtils.GetCustomGameDataObject<PoisonProvider>().ID;

            DebugLogSystem.LogVerbose(
                $"PoisonBottle registered with dedicated provider {poisonProviderId} and indisposable flag.");
        }
    }
}
