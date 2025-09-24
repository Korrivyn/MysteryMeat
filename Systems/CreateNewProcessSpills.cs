using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Generates mess requests while certain processes run on held items, simulating messy preparation steps.
    /// </summary>
    public class CreateNewProcessSpills : GameSystemBase, IModSystem
    {
        EntityQuery ItemsUndergoingProcess;

        /// <summary>
        /// Builds the query that locates items undergoing processes capable of spawning spills.
        /// </summary>
        protected override void Initialise()
        {
            base.Initialise();
            ItemsUndergoingProcess = GetEntityQuery(typeof(CItem), typeof(CHeldBy), typeof(CProcessCausesSpill), typeof(CItemUndergoingProcess));
        }

        /// <summary>
        /// Spawns mess requests when applicable processes meet their randomised spill chance thresholds.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _items = ItemsUndergoingProcess.ToEntityArray(Allocator.Temp);

            // Guard: exit early when no items are currently being processed.
            if (_items.Length == 0)
            {
                return;
            }

            foreach (Entity item in _items)
            {
                CProcessCausesSpill cProcessCausesSpill = EntityManager.GetComponentData<CProcessCausesSpill>(item);
                CItemUndergoingProcess cItemUndergoingProcess = EntityManager.GetComponentData<CItemUndergoingProcess>(item);
                CHeldBy cHeldBy = EntityManager.GetComponentData<CHeldBy>(item);
                CPosition cPosition;

                // Guard: obtain the holder position to anchor potential spill requests.
                if (!Require<CPosition>(cHeldBy.Holder, out cPosition))
                {
                    DebugLogSystem.LogWarning($"CreateNewProcessSpills could not resolve holder position for item {item.Index}; spill generation skipped.");
                    continue;
                }

                // Guard: ensure the current process matches the configured spill-causing process.
                if (cItemUndergoingProcess.Process != cProcessCausesSpill.Process)
                {
                    continue;
                }

                // Guard: skip spill generation once the process nearly completes to avoid late mess bursts.
                if (cItemUndergoingProcess.Progress >= 0.9)
                {
                    continue;
                }

                // Guard: roll against the configured spill rate to determine whether a mess should spawn.
                if (UnityEngine.Random.value < cProcessCausesSpill.Rate * Time.DeltaTime)
                {
                    Entity spill = EntityManager.CreateEntity();
                    EntityManager.AddComponentData<CMessRequest>(spill, new CMessRequest
                    {
                        ID = cProcessCausesSpill.ID,
                        OverwriteOtherMesses = cProcessCausesSpill.OverwriteOtherMesses
                    });
                    EntityManager.AddComponentData<CPosition>(spill, cPosition);
                    DebugLogSystem.LogVerbose($"CreateNewProcessSpills generated mess request {spill.Index} at position {cPosition.Position} for item {item.Index}.");
                }
            }
        }
    }
}
