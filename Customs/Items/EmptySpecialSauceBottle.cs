using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Appliances;
using KitchenMysteryMeat.Systems.Logging;
using KitchenMysteryMeat.Views;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Items
{
    /// <summary>
    /// Represents the emptied special sauce bottle that can be refilled at blood spills.
    /// </summary>
    public class EmptySpecialSauceBottle : CustomItem
    {
        public override string UniqueNameID => "EmptySpecialSauceBottle";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Empty Special Sauce Bottle").AssignMaterialsByNames();
        public override Appliance DedicatedProvider => (Appliance)GDOUtils.GetCustomGameDataObject<SpecialSauceProvider>().GameDataObject;
        public override bool IsIndisposable => true;
        public override ItemStorage ItemStorageFlags => ItemStorage.None;

        public override List<IItemProperty> Properties => new()
        {
            new CEmptyBottle
            {
                FullBottleID = GDOUtils.GetCustomGameDataObject<SpecialSauceBottle>().GameDataObject.ID
            }
        };

        /// <summary>
        /// Logs the refill target configured for empty special sauce bottles.
        /// </summary>
        /// <param name="gameDataObject">The item definition being registered.</param>
        public override void OnRegister(Item gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int fullBottleId = GDOUtils.GetCustomGameDataObject<SpecialSauceBottle>().ID;

            DebugLogSystem.LogVerbose(
                $"EmptySpecialSauceBottle registered with refill output item {fullBottleId} and dedicated provider enforcement.");
        }
    }
}
