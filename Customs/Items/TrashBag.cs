using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Appliances;
using KitchenMysteryMeat.Views;
using KitchenMysteryMeat.Systems.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Items
{
    public class TrashBag : CustomItem
    {
        public override string UniqueNameID => "TrashBag";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Trash Bag").AssignMaterialsByNames();
        public override Appliance DedicatedProvider => (Appliance)GDOUtils.GetCustomGameDataObject<TrashBagProvider>().GameDataObject;
        public override ItemStorage ItemStorageFlags => ItemStorage.OutsideRubbish;

        public override List<IItemProperty> Properties => new List<IItemProperty>()
        {
            new CTrashBag(),
            new CToolInteractionMemory(),
            new CEquippableTool()
            {
                CanHoldItems = false
            },
            new CToolStorage()
            {
                Capacity = 1
            },
        };
        /// <summary>
        /// Attaches the trash bag view when the item is registered.
        /// </summary>
        public override void OnRegister(Item gameDataObject)
        {
            base.OnRegister(gameDataObject);

            TrashBagView view = Prefab.AddComponent<TrashBagView>();
            DebugLogSystem.LogVerbose("TrashBag attached its view component during registration.");
        }
    }
}
