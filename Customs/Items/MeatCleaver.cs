﻿using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Appliances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Items
{
    class MeatCleaver : CustomItem
    {
        public override string UniqueNameID => "MeatCleaver";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Meat Cleaver").AssignMaterialsByNames().AssignVFXByNames();
        public override Appliance DedicatedProvider => (Appliance)GDOUtils.GetCustomGameDataObject<MeatCleaverProvider>().GameDataObject;
        public override ItemCategory ItemCategory => ItemCategory.Generic;
        public override ItemStorage ItemStorageFlags => ItemStorage.None;
        public override ItemValue ItemValue => ItemValue.Small;
        public override ToolAttachPoint HoldPose => ToolAttachPoint.Hand;
        public override bool IsIndisposable => true;
        public override List<IItemProperty> Properties => new()
        {
            new CProcessTool()
            {
                Process = ProcessReferences.Chop,
                Factor = 2
            },
            new CEquippableTool()
            {
                CanHoldItems = true
            },
            new CKillsCustomer()
        };
    }
}
