using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Items;
using System.Collections.Generic;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Appliances
{
    public class CustomerFloorCorpse : CustomAppliance
    {
        public override string UniqueNameID => "CustomerFloorCorpse";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Customer Floor Corpse").AssignMaterialsByNames();
        public override OccupancyLayer Layer => OccupancyLayer.Floor;

        public override List<IApplianceProperty> Properties => new List<IApplianceProperty>()
        {
            //new CDestroyApplianceAtNight(),
            KitchenPropertiesUtils.GetCItemProvider(GDOUtils.GetCustomGameDataObject<CustomerCorpse>().ID, 1, 1, false, false, false, true, false, false, false),
            new CIllegalSight()
            {
                TurnIntoOnDayStart = GDOUtils.GetCustomGameDataObject<RottenCustomerFloorCorpse>().ID
            },
            new CImmovable(),
/*
            new CTakesDuration()
            {
                Total = 0.5f,
                Active = false,
                Manual = false,
            },
            new CCausesSpills()
            {
                ID = GDOUtils.GetCustomGameDataObject<BloodSpill1>().ID,
                Rate = 2f,
                OverwriteOtherMesses = false
            }*/
        };

    }
}
