using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Items;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Appliances
{
    /// <summary>
    /// Spawns a fresh corpse that can be harvested once before decaying into a rotten variant overnight.
    /// </summary>
    public class CustomerFloorCorpse : CustomAppliance
    {
        public override string UniqueNameID => "CustomerFloorCorpse";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Customer Floor Corpse").AssignMaterialsByNames();
        public override OccupancyLayer Layer => OccupancyLayer.Floor;

        public override List<IApplianceProperty> Properties => new List<IApplianceProperty>
        {
            KitchenPropertiesUtils.GetCItemProvider(GDOUtils.GetCustomGameDataObject<CustomerCorpse>().ID, 1, 1, false, false, false, true, false, false, false),
            new CIllegalSight
            {
                TurnIntoOnDayStart = GDOUtils.GetCustomGameDataObject<RottenCustomerFloorCorpse>().ID
            },
            new CImmovable()
        };

        /// <summary>
        /// Reports the overnight decay target and provided corpse stock when the appliance registers.
        /// </summary>
        /// <param name="gameDataObject">The appliance definition being registered.</param>
        public override void OnRegister(Appliance gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int freshCorpseId = GDOUtils.GetCustomGameDataObject<CustomerCorpse>().ID;
            int rottenCorpseApplianceId = GDOUtils.GetCustomGameDataObject<RottenCustomerFloorCorpse>().ID;

            DebugLogSystem.LogVerbose(
                $"CustomerFloorCorpse registered with provider item {freshCorpseId} and overnight conversion target {rottenCorpseApplianceId}.");
        }
    }
}
