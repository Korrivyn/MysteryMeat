using Kitchen;
using KitchenData;
using KitchenLib.Customs;
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
    public class SpecialSauceBottle : CustomItem
    {
        public override string UniqueNameID => "SpecialSauceBottle";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Special Sauce Bottle").AssignMaterialsByNames();
        public override Appliance DedicatedProvider => (Appliance)GDOUtils.GetCustomGameDataObject<SpecialSauceProvider>().GameDataObject;
        public override bool IsIndisposable => true;
        public override ItemStorage ItemStorageFlags => ItemStorage.None;

        public override List<IItemProperty> Properties => new()
        {
            new CLimitedUseBottle()
            {
                FillAmount = 6,
                Limit = 6,
                EmptyBottleID = GDOUtils.GetCustomGameDataObject<EmptySpecialSauceBottle>().GameDataObject.ID
            }
        };

        /// <summary>
        /// Attaches the limited-use bottle view to render sauce fill amounts.
        /// </summary>
        public override void OnRegister(Item gameDataObject)
        {
            base.OnRegister(gameDataObject);

            LimitedUseBottleView limitedUseBottleView = Prefab.AddComponent<LimitedUseBottleView>();
            limitedUseBottleView.BottleMaterial = MaterialUtils.GetExistingMaterial("Tomato Flesh 3");
            limitedUseBottleView.LiquidMaterial = MaterialUtils.GetExistingMaterial("Clothing Red");
            DebugLogSystem.LogVerbose("SpecialSauceBottle attached LimitedUseBottleView during registration.");
        }
    }
}
