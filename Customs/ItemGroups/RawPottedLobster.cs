// Legacy lobster support remains disabled but documented for future restoration.
#if false
using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Customs.Items;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.ItemGroups
{
    /// <summary>
    /// Represents the raw lobster pot assembly that cooks into the plated lobster dish.
    /// </summary>
    public class RawPottedLobster : CustomItemGroup<ItemGroupView>
    {
        public override string UniqueNameID => "RawPottedLobster";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Raw Potted Lobster").AssignMaterialsByNames();
        public override Item DisposesTo => (Item)GDOUtils.GetExistingGDO(ItemReferences.Pot);

        public override List<ItemGroup.ItemSet> Sets => new List<ItemGroup.ItemSet>
        {
            new ItemGroup.ItemSet
            {
                Items = new List<Item>
                {
                    (Item)GDOUtils.GetExistingGDO(ItemReferences.Pot)
                },
                Min = 1,
                Max = 1,
                IsMandatory = true
            },
            new ItemGroup.ItemSet
            {
                Items = new List<Item>
                {
                    (Item)GDOUtils.GetCustomGameDataObject<RawLobster>().GameDataObject,
                    (Item)GDOUtils.GetExistingGDO(ItemReferences.Water)
                },
                Min = 2,
                Max = 2
            }
        };

        public override List<Item.ItemProcess> Processes => new List<Item.ItemProcess>
        {
            new Item.ItemProcess
            {
                Process = (Process)GDOUtils.GetExistingGDO(ProcessReferences.Cook),
                Duration = 5,
                Result = (Item)GDOUtils.GetCustomGameDataObject<CookedPottedLobster>().GameDataObject
            }
        };

        /// <summary>
        /// Configures the item group view so each component renders correctly when crafting the lobster pot.
        /// </summary>
        public override void OnRegister(ItemGroup gameDataObject)
        {
            base.OnRegister(gameDataObject);

            ItemGroupView view = gameDataObject.Prefab.GetComponent<ItemGroupView>();
            view.ComponentGroups = new List<ItemGroupView.ComponentGroup>
            {
                new ItemGroupView.ComponentGroup
                {
                    Item = (Item)GDOUtils.GetExistingGDO(ItemReferences.Pot),
                    GameObject = GameObjectUtils.GetChildObject(gameDataObject.Prefab, "Pot/Pot_1"),
                    DrawAll = true
                },
                new ItemGroupView.ComponentGroup
                {
                    Item = (Item)GDOUtils.GetExistingGDO(ItemReferences.Water),
                    GameObject = GameObjectUtils.GetChildObject(gameDataObject.Prefab, "Pot/Water"),
                    DrawAll = true
                },
                new ItemGroupView.ComponentGroup
                {
                    Item = (Item)GDOUtils.GetCustomGameDataObject<RawLobster>().GameDataObject,
                    GameObject = GameObjectUtils.GetChildObject(gameDataObject.Prefab, "Raw Lobster"),
                    DrawAll = true
                }
            };
        }
    }
}
#endif
