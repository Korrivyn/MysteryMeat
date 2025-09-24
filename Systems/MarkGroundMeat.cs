using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    [UpdateInGroup(typeof(ApplianceProcessReactionGroup))]
    /// <summary>
    /// Clears grindable flags from items once a meat grinder finishes processing them.
    /// </summary>
    public class MarkGroundMeat : GameSystemBase, IModSystem
    {
        private EntityQuery Appliances;

        /// <summary>
        /// Builds the query that identifies meat grinders completing their configured process.
        /// </summary>
        protected override void Initialise()
        {
            Appliances = GetEntityQuery(new QueryHelper()
                            .All(typeof(CMeatGrinder), typeof(CCompletedProcess), typeof(CItemHolder)));
        }

        /// <summary>
        /// Removes the grindable marker from held items when the grinder completes its grind process.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _appliances = Appliances.ToEntityArray(Allocator.TempJob);

            // Guard: exit early when no grinders completed their process this tick.
            if (_appliances.Length == 0)
            {
                return;
            }

            foreach (Entity appliance in _appliances)
            {
                CCompletedProcess cCompletedProcess = EntityManager.GetComponentData<CCompletedProcess>(appliance);
                CMeatGrinder cMeatGrinder = EntityManager.GetComponentData<CMeatGrinder>(appliance);
                CItemHolder cItemHolder = EntityManager.GetComponentData<CItemHolder>(appliance);

                // Guard: only react when the completed process matches the grinder's configured process.
                if (cCompletedProcess.Process == cMeatGrinder.GrindProcess)
                {
                    DebugLogSystem.LogVerbose($"Evaluating grinder {appliance.Index} for held item cleanup.");

                    // Guard: remove the grindable marker when the held item no longer requires grinding.
                    if (Has<CGrindable>(cItemHolder.HeldItem))
                    {
                        EntityManager.RemoveComponent<CGrindable>(cItemHolder.HeldItem);
                        DebugLogSystem.LogVerbose($"Cleared CGrindable from item {cItemHolder.HeldItem.Index} after grinding.");
                    }
                }
                else
                {
                    DebugLogSystem.LogVerbose($"Skipped appliance {appliance.Index} because process {cCompletedProcess.Process} does not match grinder configuration {cMeatGrinder.GrindProcess}.");
                }
            }
        }
    }
}
