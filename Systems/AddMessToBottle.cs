using Kitchen;
using KitchenData;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Refills or converts poison bottles when appliances complete their filling process.
    /// </summary>
    [UpdateBefore(typeof(DestroyAfterDuration))]
    public class AddMessToBottle : GameSystemBase, IModSystem
    {
        private EntityQuery ApplianceQuery;

        /// <summary>
        /// Builds the query that locates appliances currently filling bottles.
        /// </summary>
        protected override void Initialise()
        {
            ApplianceQuery = GetEntityQuery(new QueryHelper()
                            .All(typeof(CFillsBottle), typeof(CAppliance), typeof(CTakesDuration), typeof(CBeingActedOnBy)));
        }

        /// <summary>
        /// Converts empty bottles back into filled bottles when the appliance finishes its duration.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _appliances = ApplianceQuery.ToEntityArray(Allocator.TempJob);

            // Guard: exit early when there are no active appliances performing bottle fills.
            if (_appliances.Length == 0)
            {
                return;
            }

            foreach (Entity appliance in _appliances)
            {
                CAppliance cAppliance = GetComponent<CAppliance>(appliance);
                CFillsBottle cFillsBottle = GetComponent<CFillsBottle>(appliance);
                DynamicBuffer<CBeingActedOnBy> actors = GetBuffer<CBeingActedOnBy>(appliance);
                CTakesDuration duration = GetComponent<CTakesDuration>(appliance);

                // Guard: skip appliances that have not completed their work duration.
                if (!duration.Active || duration.Remaining > 0f)
                {
                    DebugLogSystem.LogVerbose($"Skipping appliance {appliance.Index} because the fill process is still running.");
                    continue;
                }

                // Guard: skip when no actors are currently interacting with the appliance.
                if (actors.IsEmpty)
                {
                    DebugLogSystem.LogVerbose($"Found no interacting actors for appliance {appliance.Index}.");
                    continue;
                }

                for (int i = 0; i < actors.Length; i++)
                {
                    // Guard: ensure the actor exposes an item holder before attempting to refill bottles.
                    if (Require<CItemHolder>(actors[i].Interactor, out var itemHolder))
                    {
                        /*if (GetComponent<CItem>(itemHolder.HeldItem).ID != cFillsBottle.BottleID)
                            continue;*/

                        // Guard: replace empty bottles with their filled counterparts when discovered.
                        if (Require<CEmptyBottle>(itemHolder.HeldItem, out var emptyBottle))
                        {
                            EntityManager.AddComponentData<CChangeItemType>(itemHolder.HeldItem, new CChangeItemType()
                            {
                                NewID = emptyBottle.FullBottleID,
                            });
                            EntityManager.RemoveComponent<CEmptyBottle>(itemHolder.HeldItem);
                            DebugLogSystem.LogVerbose($"Converted empty bottle entity {itemHolder.HeldItem.Index} to full bottle ID {emptyBottle.FullBottleID}.");
                            continue;
                        }

                        // Guard: refill partially used bottles by resetting their fill count to the configured limit.
                        if (Require<CLimitedUseBottle>(itemHolder.HeldItem, out var bottle))
                        {
                            bottle.FillAmount = bottle.Limit;

                            if (actors[i].Interactor != Entity.Null)
                            {
                                EntityManager.SetComponentData(itemHolder.HeldItem, bottle);
                            }

                            DebugLogSystem.LogVerbose($"Refilled limited-use bottle entity {itemHolder.HeldItem.Index} to limit {bottle.Limit}.");
                        }
                    }
                    else
                    {
                        int actorIndex = actors[i].Interactor != Entity.Null ? actors[i].Interactor.Index : -1;
                        DebugLogSystem.LogWarning($"Encountered actor {actorIndex} without a CItemHolder while refilling bottles on appliance {appliance.Index}.");
                    }
                }

                EntityManager.RemoveComponent<CFillsBottle>(appliance);
                DebugLogSystem.LogVerbose($"Cleared CFillsBottle from appliance {appliance.Index} after refilling bottles.");
            }
        }
    }
}
