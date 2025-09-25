// This placeholder remains disabled until the bagged corpse workflow returns; summary comments remain for future reactivation.
#if false
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Customs.Items;
using System.Collections.Generic;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.ItemGroups
{
    /// <summary>
    /// Represents a corpse packaged inside a trash bag that splits back into its components when processed.
    /// </summary>
    internal class BaggedCorpse : CustomItemGroup<ItemGroupView>
    {
        public override string UniqueNameID => "BaggedCorpse";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Bagged Corpse").AssignMaterialsByNames();
        public override ItemCategory ItemCategory => ItemCategory.Generic;
        public override ItemStorage ItemStorageFlags => ItemStorage.OutsideRubbish;
        public override ItemValue ItemValue => ItemValue.Medium;
        public override Item SplitSubItem => (Item)GDOUtils.GetCustomGameDataObject<TrashBag>().GameDataObject;
        public override List<Item> SplitDepletedItems => new() { (Item)GDOUtils.GetCustomGameDataObject<CustomerCorpse>().GameDataObject };
        public override int SplitCount => 1;
        public override float SplitSpeed => 3.0f;
        public override List<ItemGroup.ItemSet> Sets => new List<ItemGroup.ItemSet>
        {
            new ItemGroup.ItemSet
            {
                Max = 2,
                Min = 2,
                Items = new List<Item>
                {
                    (Item)GDOUtils.GetCustomGameDataObject<CustomerCorpse>().GameDataObject,
                    (Item)GDOUtils.GetCustomGameDataObject<TrashBag>().GameDataObject,
                }
            }
        };
    }
}
#endif
