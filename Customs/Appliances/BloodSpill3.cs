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
    /// Represents the final blood spill escalation that demands the longest cleanup time and still refills bottles.
    /// </summary>
    public class BloodSpill3 : CustomAppliance
    {
        public override string UniqueNameID => "BloodSpill3";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Mess - Blood Spill 3").AssignMaterialsByNames();
        public override OccupancyLayer Layer => OccupancyLayer.Floor;
        public override EntryAnimation EntryAnimation => EntryAnimation.Mess;
        public override ExitAnimation ExitAnimation => ExitAnimation.MessDestroy;

        public override List<IApplianceProperty> Properties => new List<IApplianceProperty>
        {
            new CSlowPlayer
            {
                Radius = 0.3f,
                Factor = 0.9f
            },
            new CTakesDuration
            {
                Total = 6,
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
            new CIllegalSight(),
            new CFillsBottle
            {
                BottleID = GDOUtils.GetCustomGameDataObject<SpecialSauceBottle>().ID
            }
        };

        /// <summary>
        /// Logs the clean-up burden for the final spill tier while confirming refill support.
        /// </summary>
        /// <param name="gameDataObject">The appliance definition being registered.</param>
        public override void OnRegister(Appliance gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int refillBottleId = GDOUtils.GetCustomGameDataObject<SpecialSauceBottle>().ID;

            DebugLogSystem.LogVerbose(
                $"BloodSpill3 registered as the terminal tier with refill bottle ID {refillBottleId} and a 6 second clean time.");
        }
    }
}
