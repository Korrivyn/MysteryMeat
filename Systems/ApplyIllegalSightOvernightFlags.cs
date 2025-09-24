using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Applies overnight preservation markers to illegal sight holders and manages appliance destruction flags.
    /// </summary>
    public class ApplyIllegalSightOvernightFlags : GameSystemBase, IModSystem
    {
        private EntityQuery IllegalEntities;
        private EntityQuery HolderPreservers;

        /// <summary>
        /// Configures queries for illegal entities and holders previously marked for overnight preservation.
        /// </summary>
        protected override void Initialise()
        {
            base.Initialise();

            IllegalEntities = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CIllegalSight>() }
            });

            HolderPreservers = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CIllegalSightHolderPreserved>() }
            });
        }

        /// <summary>
        /// Applies overnight preservation based on the persistent corpses status and cleans up stale markers.
        /// </summary>
        protected override void OnUpdate()
        {
            bool persistent = HasStatus((RestaurantStatus)VariousUtils.GetID("persistentcorpses"));

            HashSet<Entity> holdersWithIllegalItems = persistent ? new HashSet<Entity>() : null;

            using NativeArray<Entity> illegals = IllegalEntities.ToEntityArray(Allocator.Temp);

            // Guard: log when no illegal entities were discovered during the overnight evaluation.
            if (illegals.Length == 0)
            {
                DebugLogSystem.LogVerbose("Found no illegal entities to process during overnight cleanup.");
            }

            for (int i = 0; i < illegals.Length; ++i)
            {
                Entity entity = illegals[i];

                // Guard: when persistence is enabled, ensure holders storing illegal items retain their contents overnight.
                if (persistent && EntityManager.HasComponent<CItem>(entity))
                {
                    HandleHeldItemPreservation(entity, holdersWithIllegalItems);
                }

                // Always evaluate appliance behaviour to ensure destruction flags remain accurate.
                HandleApplianceOvernightBehaviour(entity, persistent);
            }

            CleanupHolderPreservers(persistent, holdersWithIllegalItems);
        }

        /// <summary>
        /// Ensures holders carrying illegal items retain overnight preservation when persistence is enabled.
        /// </summary>
        private void HandleHeldItemPreservation(Entity item, HashSet<Entity> holdersWithIllegalItems)
        {
            // Guard: skip when persistence tracking is not active for holders.
            if (holdersWithIllegalItems == null)
            {
                return;
            }

            // Guard: only proceed when the item is currently held by another entity.
                if (!EntityManager.HasComponent<CHeldBy>(item))
                {
                    DebugLogSystem.LogVerbose($"Skipped illegal item {item.Index} because it is not held by an appliance.");
                    return;
                }

            CHeldBy heldBy = EntityManager.GetComponentData<CHeldBy>(item);
            Entity holder = heldBy.Holder;

            // Guard: verify the holder is a valid appliance before adding overnight preservation.
            if (holder == Entity.Null || !EntityManager.HasComponent<CAppliance>(holder))
            {
                DebugLogSystem.LogWarning($"Detected illegal item {item.Index} held by invalid entity {holder.Index}; preservation skipped.");
                return;
            }

            holdersWithIllegalItems.Add(holder);

            bool hadPreserver = EntityManager.HasComponent<CPreservesContentsOvernight>(holder);
            if (!hadPreserver)
            {
                EntityManager.AddComponentData(holder, new CPreservesContentsOvernight());
                DebugLogSystem.LogVerbose($"Added overnight preservation to holder {holder.Index}.");
            }

            if (EntityManager.HasComponent<CIllegalSightHolderPreserved>(holder))
            {
                if (!hadPreserver)
                {
                    CIllegalSightHolderPreserved marker = EntityManager.GetComponentData<CIllegalSightHolderPreserved>(holder);
                    if (!marker.AddedPreserver)
                    {
                        marker.AddedPreserver = true;
                        EntityManager.SetComponentData(holder, marker);
                        DebugLogSystem.LogVerbose($"Updated holder preservation marker for entity {holder.Index} to reflect added preserver.");
                    }
                }
            }
            else
            {
                EntityManager.AddComponentData(holder, new CIllegalSightHolderPreserved
                {
                    AddedPreserver = !hadPreserver
                });
                DebugLogSystem.LogVerbose($"Created preservation marker for holder {holder.Index}.");
            }
        }

        /// <summary>
        /// Removes stale preservation markers from holders that no longer require overnight protection.
        /// </summary>
        private void CleanupHolderPreservers(bool persistent, HashSet<Entity> holdersWithIllegalItems)
        {
            using NativeArray<Entity> holders = HolderPreservers.ToEntityArray(Allocator.Temp);
            if (holders.Length == 0)
            {
                return;
            }

            for (int i = holders.Length - 1; i >= 0; --i)
            {
                Entity holder = holders[i];

                // Guard: retain the preserver when persistence is active and the holder still contains illegal items.
                if (persistent && holdersWithIllegalItems != null && holdersWithIllegalItems.Contains(holder))
                {
                    DebugLogSystem.LogVerbose($"Retained overnight preservation for holder {holder.Index} because persistent corpses are enabled.");
                    continue;
                }

                CIllegalSightHolderPreserved marker = EntityManager.GetComponentData<CIllegalSightHolderPreserved>(holder);

                if (marker.AddedPreserver && EntityManager.HasComponent<CPreservesContentsOvernight>(holder))
                {
                    EntityManager.RemoveComponent<CPreservesContentsOvernight>(holder);
                    DebugLogSystem.LogVerbose($"Removed overnight preserver from holder {holder.Index}.");
                }

                EntityManager.RemoveComponent<CIllegalSightHolderPreserved>(holder);
                DebugLogSystem.LogVerbose($"Cleared preservation marker from holder {holder.Index}.");
            }
        }

        /// <summary>
        /// Updates appliance overnight behaviour, ensuring destruction flags align with persistence settings.
        /// </summary>
        private void HandleApplianceOvernightBehaviour(Entity entity, bool persistent)
        {
            if (!EntityManager.HasComponent<CAppliance>(entity))
            {
                return;
            }

            bool hasDestroyMarker = EntityManager.HasComponent<CDestroyApplianceAtNight>(entity);

            if (persistent)
            {
                CIllegalSight illegalSight = EntityManager.GetComponentData<CIllegalSight>(entity);
                bool transformsOnDayStart = illegalSight.TurnIntoOnDayStart > 0;

                if (transformsOnDayStart)
                {
                    if (hasDestroyMarker)
                    {
                        EntityManager.RemoveComponent<CDestroyApplianceAtNight>(entity);
                        DebugLogSystem.LogVerbose($"Removed nighttime destruction from appliance {entity.Index} because it transforms on day start.");
                    }
                }

                return;
            }

            if (!hasDestroyMarker)
            {
                EntityManager.AddComponentData(entity, new CDestroyApplianceAtNight());
                DebugLogSystem.LogVerbose($"Marked appliance {entity.Index} for destruction at night.");
            }
        }
    }
}
