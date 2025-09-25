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
    /// Represents the mid-stage blood spill that deepens slowing effects and advances to the final mess tier.
    /// </summary>
    public class BloodSpill2 : CustomAppliance
    {
        public override string UniqueNameID => "BloodSpill2";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Mess - Blood Spill 2").AssignMaterialsByNames();
        public override OccupancyLayer Layer => OccupancyLayer.Floor;
        public override EntryAnimation EntryAnimation => EntryAnimation.Mess;
        public override ExitAnimation ExitAnimation => ExitAnimation.MessDestroy;

        public override List<IApplianceProperty> Properties => new List<IApplianceProperty>
        {
            new CSlowPlayer
            {
                Radius = 0.25f,
                Factor = 1.0f
            },
            new CTakesDuration
            {
                Total = 3,
                Manual = true,
                ManualNeedsEmptyHands = false,
                RelevantTool = DurationToolType.Clean,
                Mode = InteractionMode.Items
            },
            new CDestroyAfterDuration(),
            new CDestroyApplianceAtNight(),
            new CDisplayDuration
            {
                IsBad = false,
                Process = ProcessReferences.Clean,
                ShowWhenEmpty = false
            },
            new CStackableMess
            {
                BaseMess = GDOUtils.GetCustomGameDataObject<BloodSpill1>().ID,
                NextMess = GDOUtils.GetCustomGameDataObject<BloodSpill3>().ID
            },
            new CIllegalSight(),
            new CFillsBottle
            {
                BottleID = GDOUtils.GetCustomGameDataObject<SpecialSauceBottle>().ID
            }
        };

        /// <summary>
        /// Emits verbose diagnostics about the escalation path and refill behaviour for the second blood spill tier.
        /// </summary>
        /// <param name="gameDataObject">The appliance definition being registered.</param>
        public override void OnRegister(Appliance gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int previousMessId = GDOUtils.GetCustomGameDataObject<BloodSpill1>().ID;
            int nextMessId = GDOUtils.GetCustomGameDataObject<BloodSpill3>().ID;
            int refillBottleId = GDOUtils.GetCustomGameDataObject<SpecialSauceBottle>().ID;

            DebugLogSystem.LogVerbose(
                $"BloodSpill2 registered with previous mess {previousMessId}, next mess {nextMessId}, and refill bottle ID {refillBottleId}.");
        }
    }
}
