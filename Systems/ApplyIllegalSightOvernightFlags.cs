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
        private EntityQuery IllegalEntities;
        private EntityQuery HolderPreservers;

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

        protected override void OnUpdate()
        {
            bool persistent = HasStatus((RestaurantStatus)VariousUtils.GetID("persistentcorpses"));

            HashSet<Entity> holdersWithIllegalItems = persistent ? new HashSet<Entity>() : null;

            using NativeArray<Entity> illegals = IllegalEntities.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < illegals.Length; ++i)
            {
                Entity entity = illegals[i];
                CIllegalSight illegal = EntityManager.GetComponentData<CIllegalSight>(entity);
                bool canTurn = illegal.TurnIntoOnDayStart > 0;
                bool hasPreservedFlag = EntityManager.HasComponent<CPreservedOvernight>(entity);

                if (EntityManager.HasComponent<CItem>(entity))
                {
                    if (persistent)
                    {
                        if (canTurn && !hasPreservedFlag)
                        {
                            EntityManager.AddComponentData(entity, new CPreservedOvernight());
                            hasPreservedFlag = true;
                        }

                        if ((canTurn || hasPreservedFlag) && holdersWithIllegalItems != null)
                        {
                            HandleHeldItemPreservation(entity, holdersWithIllegalItems);
                        }
                    }
                    else if (canTurn && hasPreservedFlag)
                    {
                        EntityManager.RemoveComponent<CPreservedOvernight>(entity);
                    }
                }

                if (EntityManager.HasComponent<CAppliance>(entity))
                {
                    if (persistent)
                    {
                        if (EntityManager.HasComponent<CDestroyApplianceAtNight>(entity))
                        {
                            EntityManager.RemoveComponent<CDestroyApplianceAtNight>(entity);
                        }
                    }
                    else if (canTurn && !EntityManager.HasComponent<CDestroyApplianceAtNight>(entity))
                    {
                        EntityManager.AddComponentData(entity, new CDestroyApplianceAtNight());
                    }
                }
            }

            CleanupHolderPreservers(persistent, holdersWithIllegalItems);
        }

        private void HandleHeldItemPreservation(Entity item, HashSet<Entity> holdersWithIllegalItems)
        {
            if (holdersWithIllegalItems == null)
                return;

            if (!EntityManager.HasComponent<CHeldBy>(item))
                return;

            CHeldBy heldBy = EntityManager.GetComponentData<CHeldBy>(item);
            Entity holder = heldBy.Holder;

            if (holder == Entity.Null || !EntityManager.HasComponent<CAppliance>(holder))
                return;

            holdersWithIllegalItems.Add(holder);

            bool hadPreserver = EntityManager.HasComponent<CPreservesContentsOvernight>(holder);
            if (!hadPreserver)
            {
                EntityManager.AddComponentData(holder, new CPreservesContentsOvernight());
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
                    }
                }
            }
            else
            {
                EntityManager.AddComponentData(holder, new CIllegalSightHolderPreserved
                {
                    AddedPreserver = !hadPreserver
                });
            }
        }

        private void CleanupHolderPreservers(bool persistent, HashSet<Entity> holdersWithIllegalItems)
        {
            using NativeArray<Entity> holders = HolderPreservers.ToEntityArray(Allocator.Temp);
            if (holders.Length == 0)
                return;

            for (int i = holders.Length - 1; i >= 0; --i)
            {
                Entity holder = holders[i];

                if (persistent && holdersWithIllegalItems != null && holdersWithIllegalItems.Contains(holder))
                    continue;

                CIllegalSightHolderPreserved marker = EntityManager.GetComponentData<CIllegalSightHolderPreserved>(holder);

                if (marker.AddedPreserver && EntityManager.HasComponent<CPreservesContentsOvernight>(holder))
                {
                    EntityManager.RemoveComponent<CPreservesContentsOvernight>(holder);
                }

                EntityManager.RemoveComponent<CIllegalSightHolderPreserved>(holder);
            }
        }
    }
}
