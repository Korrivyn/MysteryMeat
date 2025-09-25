using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Items;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Appliances
{
    /// <summary>
    /// Represents the rotten corpse variant that remains after an uncleaned body decomposes overnight.
    /// </summary>
    public class RottenCustomerFloorCorpse : CustomAppliance
    {
        public override string UniqueNameID => "RottenCustomerFloorCorpse";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Rotten Customer Floor Corpse").AssignMaterialsByNames().AssignVFXByNames();
        public override OccupancyLayer Layer => OccupancyLayer.Floor;

        public override List<IApplianceProperty> Properties => new List<IApplianceProperty>
        {
            KitchenPropertiesUtils.GetCItemProvider(GDOUtils.GetCustomGameDataObject<RottenCustomerCorpse>().ID, 1, 1, false, false, false, true, false, false, false),
            new CIllegalSight(),
            new CImmovable()
        };

        /// <summary>
        /// Emits diagnostics confirming the rotten corpse provider stock during registration.
        /// </summary>
        /// <param name="gameDataObject">The appliance definition being registered.</param>
        public override void OnRegister(Appliance gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int rottenCorpseItemId = GDOUtils.GetCustomGameDataObject<RottenCustomerCorpse>().ID;

            DebugLogSystem.LogVerbose(
                $"RottenCustomerFloorCorpse registered with provider item {rottenCorpseItemId} and immovable illegal sight status.");
        }
    }
}
