using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    public class ApplyIllegalSightOvernightFlags : GameSystemBase, IModSystem
    {
        private EntityQuery IllegalSightEntities;
        private EntityQuery TemporarilyPreservingHolders;

        protected override void Initialise()
        {
            base.Initialise();

            IllegalSightEntities = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CIllegalSight>() }
            });

            TemporarilyPreservingHolders = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CPersistentCorpseHolder>() }
            });
        }

        protected override void OnUpdate()
        {
            bool persistentCorpsesActive = HasStatus((RestaurantStatus)VariousUtils.GetID("persistentcorpses"));

            using NativeArray<Entity> illegalSightItems = IllegalSightEntities.ToEntityArray(Allocator.Temp);
            // Track appliances that we grant temporary preservation to this frame so that the
            // cleanup pass only removes the marker from holders no longer associated with a corpse.
            HashSet<Entity> holdersReceivingTemporaryPreservation = new HashSet<Entity>();

            for (int i = 0; i < illegalSightItems.Length; ++i)
            {
                Entity illegalEntity = illegalSightItems[i];
                CIllegalSight illegalSight = EntityManager.GetComponentData<CIllegalSight>(illegalEntity);

                if (illegalSight.TurnIntoOnDayStart <= 0)
                    continue;

                List<Entity> holderEntities = CollectHolderEntities(illegalEntity);

                if (EntityManager.HasComponent<CItem>(illegalEntity))
                {
                    if (persistentCorpsesActive)
                    {
                        if (!EntityManager.HasComponent<CPreservedOvernight>(illegalEntity))
                        {
                            EntityManager.AddComponentData(illegalEntity, new CPreservedOvernight());
                        }
                    }
                    else if (EntityManager.HasComponent<CPreservedOvernight>(illegalEntity))
                    {
                        EntityManager.RemoveComponent<CPreservedOvernight>(illegalEntity);
                    }
                }

                if (EntityManager.HasComponent<CAppliance>(illegalEntity))
                {
                    if (persistentCorpsesActive)
                    {
                        if (EntityManager.HasComponent<CDestroyApplianceAtNight>(illegalEntity))
                        {
                            EntityManager.RemoveComponent<CDestroyApplianceAtNight>(illegalEntity);
                        }
                    }
                    else if (!EntityManager.HasComponent<CDestroyApplianceAtNight>(illegalEntity))
                    {
                        EntityManager.AddComponentData(illegalEntity, new CDestroyApplianceAtNight());
                    }
                }

                for (int h = 0; h < holderEntities.Count; h++)
                {
                    Entity holderEntity = holderEntities[h];
                    if (holderEntity == Entity.Null || !EntityManager.Exists(holderEntity))
                        continue;

                    if (!EntityManager.HasComponent<CAppliance>(holderEntity))
                        continue;

                    if (persistentCorpsesActive)
                    {
                        if (!EntityManager.HasComponent<CPreservesContentsOvernight>(holderEntity))
                        {
                            EntityManager.AddComponentData(holderEntity, new CPreservesContentsOvernight());

                            if (!EntityManager.HasComponent<CPersistentCorpseHolder>(holderEntity))
                            {
                                // Tag this appliance so later systems know its preservation status
                                // was provided temporarily by Persistent Corpses.
                                EntityManager.AddComponentData(holderEntity, new CPersistentCorpseHolder());
                            }
                        }

                        holdersReceivingTemporaryPreservation.Add(holderEntity);
                    }
                }
            }

            ReconcileTemporaryHolderFlags(persistentCorpsesActive, holdersReceivingTemporaryPreservation);
        }

        private List<Entity> CollectHolderEntities(Entity storedEntity)
        {
            List<Entity> holderEntities = new List<Entity>(2);

            if (EntityManager.HasComponent<CHeldBy>(storedEntity))
            {
                CHeldBy heldBy = EntityManager.GetComponentData<CHeldBy>(storedEntity);
                if (heldBy.Holder != Entity.Null)
                {
                    holderEntities.Add(heldBy.Holder);
                }
            }

            if (EntityManager.HasComponent<CStoredBy>(storedEntity))
            {
                CStoredBy storedBy = EntityManager.GetComponentData<CStoredBy>(storedEntity);
                Entity storedByEntity = storedBy.StoredBy;

                if (storedByEntity != Entity.Null && !holderEntities.Contains(storedByEntity))
                {
                    holderEntities.Add(storedByEntity);
                }
            }

            return holderEntities;
        }

        private void ReconcileTemporaryHolderFlags(bool persistentCorpsesActive, HashSet<Entity> holdersReceivingTemporaryPreservation)
        {
            // Remove temporary preservation from any appliance that was not refreshed this frame or
            // once the Persistent Corpses status is no longer active.
            using NativeArray<Entity> flaggedHolders = TemporarilyPreservingHolders.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < flaggedHolders.Length; i++)
            {
                Entity holderEntity = flaggedHolders[i];

                if (holderEntity == Entity.Null || !EntityManager.Exists(holderEntity))
                    continue;

                if (persistentCorpsesActive && holdersReceivingTemporaryPreservation.Contains(holderEntity))
                    continue;

                if (EntityManager.HasComponent<CPersistentCorpseHolder>(holderEntity))
                {
                    EntityManager.RemoveComponent<CPersistentCorpseHolder>(holderEntity);
                }

                if (EntityManager.HasComponent<CPreservesContentsOvernight>(holderEntity))
                {
                    EntityManager.RemoveComponent<CPreservesContentsOvernight>(holderEntity);
                }
            }
        }
    }
}
