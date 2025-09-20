using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Appliances;
using System.Collections.Generic;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Items
{
    public class CustomerCorpseItemView : ObjectsSplittableView
    {
        internal void Setup(GameObject prefab)
        {
            var fObjects = ReflectionUtils.GetField<ObjectsSplittableView>("Objects");
            fObjects.SetValue(this, new List<GameObject>()
            {
                prefab.GetChild("CorpseStage5"),
                prefab.GetChild("CorpseStage4"),
                prefab.GetChild("CorpseStage3"),
                prefab.GetChild("CorpseStage2"),
                prefab.GetChild("Corpse"),
            });
        }
    }

    public class CustomerCorpse : CustomItem
    {
        // UniqueNameID - This is used internally to generate the ID of this GDO. Once you've set it, don't change it.
        public override string UniqueNameID => "CustomerCorpse";

        // Prefab - This is the GameObject used for this Item's visual. AssignMaterialsByNames() is a helper method that assigns materials to the GameObject based on the names of the materials.
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Customer Corpse").AssignMaterialsByNames();
        public override Item SplitSubItem => (Item)GDOUtils.GetCustomGameDataObject<MysteryMeat>().GameDataObject;
        public override int SplitCount => 5;
        public override float SplitSpeed => 1.0f;
        public override List<Item> SplitDepletedItems => new() { (Item)GDOUtils.GetCustomGameDataObject<MysteryMeat>().GameDataObject };
        public override ItemStorage ItemStorageFlags => ItemStorage.OutsideRubbish;
        public override bool IsIndisposable => true;
        public override List<IItemProperty> Properties => new()
        {
            new CProcessCausesSpill()
            {
                Process = -1,
                ID = GDOUtils.GetCustomGameDataObject<BloodSpill1>().ID,
                Rate = 1.0f,
                OverwriteOtherMesses = false
            },
            new CIllegalSight()
            {
                TurnIntoOnDayStart = GDOUtils.GetCustomGameDataObject<RottenCustomerCorpse>().ID
            },
            new CPreventItemMerge()
            {
                Condition = MergeCondition.OnlyAsFirstSplitElement
            },
        };

        public override void OnRegister(Item item)
        {
            if (!Prefab.HasComponent<CustomerCorpseItemView>())
            {
                var view = Prefab.AddComponent<CustomerCorpseItemView>();
                view.Setup(Prefab);
            }

        }
    }
}
