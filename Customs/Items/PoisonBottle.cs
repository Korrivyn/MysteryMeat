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
    }
}
