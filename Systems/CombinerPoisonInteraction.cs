using Kitchen;
using KitchenData;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenMysteryMeat.Systems
{
    [UpdateAfter(typeof(AttemptInteraction))]
    [UpdateInGroup(typeof(InteractionGroup), OrderFirst = true)]
    /// <summary>
    /// Coordinates automated interactors that wield poison bottles so they apply poison to reachable occupants
    /// positioned in front of them, mirroring the poisoning behaviour a player would perform manually.
    /// </summary>
    public class CombinerPoisonInteraction : GenericSystemBase, IModSystem
    {
        private EntityQuery InteractivesQuery;

        /// <summary>
        /// Configures the entity query that gathers automated interactors equipped with the required spatial and holder data.
        /// </summary>
        protected override void Initialise()
        {
            base.Initialise();
            this.InteractivesQuery = GetEntityQuery(new QueryHelper()
                            .All(typeof(CAutomatedInteractor), typeof(CPosition), typeof(CItemHolder)));
        }

        /// <summary>
        /// Applies poison from automated interactors holding poison bottles to valid forward targets when the tile is reachable
        /// and the occupant carries an unpoisoned item.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> automatedInteractors = InteractivesQuery.ToEntityArray(Allocator.Temp);

            // Iterate through each automated interactor to evaluate poisoning opportunities.
            foreach (Entity automatedInteractor in automatedInteractors)
            {
                DebugLogSystem.LogVerbose($"Processing automated interactor {automatedInteractor.Index} for poison evaluation.");
                CPosition position = GetComponent<CPosition>(automatedInteractor);
                CAutomatedInteractor auto = GetComponent<CAutomatedInteractor>(automatedInteractor);
                CItemHolder itemHolder = GetComponent<CItemHolder>(automatedInteractor);

                // Guard: skip when the interactor does not currently hold a poison bottle.
                if (!Has<CPoisonBottle>(itemHolder.HeldItem))
                {
                    continue;
                }

                // Guard: skip when the interactor is inactive because its random interval is disabled.
                if (base.Require<CAutomatedInteractorRandomActiveInterval>(automatedInteractor, out CAutomatedInteractorRandomActiveInterval randomInterval) && !randomInterval.Active)
                {
                    DebugLogSystem.LogVerbose($"Skipping interactor {automatedInteractor.Index} because the random interval is inactive.");
                    continue;
                }

                Vector3 forwardPosition = position.ForwardPosition;
                Entity occupant = TileManager.GetOccupant(forwardPosition, OccupancyLayer.Default);

                // Guard: warn when the interactor cannot reach the tile in front of it.
                if (!TileManager.CanReach(position, forwardPosition, false))
                {
                    DebugLogSystem.LogWarning($"Automated interactor {automatedInteractor.Index} cannot reach tile at {forwardPosition} while attempting to poison.");
                    continue;
                }

                // Guard: capture verbose breadcrumb when no occupant is available to interact with.
                if (occupant == Entity.Null)
                {
                    DebugLogSystem.LogVerbose($"Automated interactor {automatedInteractor.Index} found no occupant at {forwardPosition} when attempting to poison.");
                    continue;
                }

                // Request the interaction attempt so downstream systems perform the grab logic.
                EntityManager.AddComponentData<CAttemptingInteraction>(automatedInteractor, new CAttemptingInteraction
                {
                    Target = occupant,
                    Type = auto.Type,
                    IsHeld = auto.IsHeld,
                    Location = forwardPosition,
                    Mode = InteractionMode.Items,
                    TransferOnly = auto.TransferOnly
                });

                // Branch: attempt to poison only when the interactor is performing a grab interaction.
                if (auto.Type == InteractionType.Grab)
                {
                    // Guard: ensure the occupant holds an unpoisoned item before applying poison.
                    if (base.Require<CItemHolder>(occupant, out CItemHolder occupantItem) && occupantItem.HeldItem != Entity.Null && !base.Has<CPoisoned>(occupantItem.HeldItem))
                    {
                        EntityManager.AddComponent<CPoisoned>(occupantItem.HeldItem);
                        CSoundEvent.Create(EntityManager, Mod.PoisonSoundEvent);
                        DebugLogSystem.LogInfo($"Applied poison to entity {occupantItem.HeldItem.Index} using interactor {automatedInteractor.Index}.");
                    }
                    else
                    {
                        DebugLogSystem.LogVerbose($"Interactor {automatedInteractor.Index} found no valid item to poison on occupant {occupant.Index}.");
                    }
                }
            }
        }
    }
}
