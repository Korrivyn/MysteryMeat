using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Appliances;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Items
{
    /// <summary>
    /// Provides the meat cleaver tool used for chopping processes and lethal interactions.
    /// </summary>
    public class MeatCleaver : CustomItem
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
            new CProcessTool
            {
                Process = ProcessReferences.Chop,
                Factor = 2
            },
            new CEquippableTool
            {
                CanHoldItems = true
            },
            new CKillsCustomer()
        };

        /// <summary>
        /// Emits verbose diagnostics about the cleaver's kill flag and process speed multiplier.
        /// </summary>
        /// <param name="gameDataObject">The item definition being registered.</param>
        public override void OnRegister(Item gameDataObject)
        {
            base.OnRegister(gameDataObject);

            DebugLogSystem.LogVerbose("MeatCleaver registered with lethal interactions and double-speed chopping capability.");
        }
    }
}
