using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Items;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.ItemGroups
{
    /// <summary>
    /// Represents the raw hotdog assembly composed of minced meat and casings before cooking.
    /// </summary>
    public class RawHotdog : CustomItemGroup
    {
        public override string UniqueNameID => "RawHotdogItemGroup";
        public override GameObject Prefab => ((Item)GDOUtils.GetExistingGDO(ItemReferences.HotdogRaw)).Prefab;
        public override bool AutoCollapsing => true;

        public override List<ItemGroup.ItemSet> Sets => new List<ItemGroup.ItemSet>
        {
            new ItemGroup.ItemSet
            {
                Max = 1,
                Min = 1,
                IsMandatory = true,
                Items = new List<Item>
                {
                    (Item)GDOUtils.GetExistingGDO(ItemReferences.Mince)
                }
            },
            new ItemGroup.ItemSet
            {
                Max = 1,
                Min = 1,
                IsMandatory = true,
                Items = new List<Item>
                {
                    GDOUtils.GetCastedGDO<Item, Casing>()
                }
            }
        };

        public override List<IItemProperty> Properties => new List<IItemProperty>
        {
            new CTurnIntoItem
            {
                NewID = ItemReferences.HotdogRaw
            }
        };

        /// <summary>
        /// Emits verbose diagnostics showing the ingredient IDs that build the raw hotdog assembly.
        /// </summary>
        /// <param name="gameDataObject">The item group definition being registered.</param>
        public override void OnRegister(ItemGroup gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int minceId = GDOUtils.GetExistingGDO(ItemReferences.Mince).ID;
            int casingId = GDOUtils.GetCustomGameDataObject<Casing>().ID;

            DebugLogSystem.LogVerbose(
                $"RawHotdog registered with mince item {minceId} and casing item {casingId} as mandatory sets.");
        }
    }
}
