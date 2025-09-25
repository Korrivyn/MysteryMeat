using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Appliances;
using KitchenMysteryMeat.Customs.Processes;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Items
{
    /// <summary>
    /// Defines the core mystery meat ingredient that can be chopped or ground into other recipes.
    /// </summary>
    public class MysteryMeat : CustomItem
    {
        public override string UniqueNameID => "MysteryMeat";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Mystery Meat").AssignMaterialsByNames();
        public override Appliance DedicatedProvider => (Appliance)GDOUtils.GetCustomGameDataObject<MeatCleaverProvider>().GameDataObject;
        public override ItemStorage ItemStorageFlags => ItemStorage.StackableFood;

        public override List<Item.ItemProcess> Processes => new List<Item.ItemProcess>
        {
            new Item.ItemProcess
            {
                Process = (Process)GDOUtils.GetExistingGDO(ProcessReferences.Chop),
                Duration = 1.0f,
                Result = (Item)GDOUtils.GetExistingGDO(ItemReferences.MeatChopped)
            },
            new Item.ItemProcess
            {
                Duration = 2.0f,
                Process = GDOUtils.GetCastedGDO<Process, GrindMeat>(),
                Result = (Item)GDOUtils.GetExistingGDO(ItemReferences.Mince)
            }
        };

        public override List<IItemProperty> Properties => new List<IItemProperty>
        {
            new CGrindable()
        };

        /// <summary>
        /// Emits verbose diagnostics about the chopping and grinding routes for mystery meat.
        /// </summary>
        /// <param name="gameDataObject">The item definition being registered.</param>
        public override void OnRegister(Item gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int grindProcessId = GDOUtils.GetCustomGameDataObject<GrindMeat>().ID;

            DebugLogSystem.LogVerbose(
                $"MysteryMeat registered with chop and grind outputs, including grind process {grindProcessId} targeting minced meat.");
        }
    }
}
