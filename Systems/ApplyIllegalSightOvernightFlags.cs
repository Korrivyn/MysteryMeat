using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Items;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    public class ApplyIllegalSightOvernightFlags : GameSystemBase, IModSystem
    {
        private EntityQuery IllegalSightEntities;
        private EntityQuery TemporarilyPreservingHolders;

        private static HashSet<int> CorpseItemIDs;

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

            EnsureCorpseIDCache();
        }

        protected override void OnUpdate()
        {
            bool persistentCorpsesActive = HasStatus((RestaurantStatus)VariousUtils.GetID("persistentcorpses"));

            using NativeArray<Entity> illegalSightItems = IllegalSightEntities.ToEntityArray(Allocator.Temp);
            // Track appliances that we grant temporary preservation to this frame so that the
            // cleanup pass only removes the marker from holders no longer associated with a corpse.
            HashSet<Entity> holdersReceivingTemporaryPreservation = new HashSet<Entity>();
            List<Entity> holderBuffer = new List<Entity>(capacity: 2);

            for (int i = 0; i < illegalSightItems.Length; ++i)
            {
                Entity illegalEntity = illegalSightItems[i];
                CIllegalSight illegalSight = EntityManager.GetComponentData<CIllegalSight>(illegalEntity);

                bool isCorpseItem = IsCorpseItem(illegalEntity);
                if (isCorpseItem)
                {
                    HandleCorpseItem(illegalEntity, persistentCorpsesActive, holdersReceivingTemporaryPreservation, holderBuffer);
                }
                else if (EntityManager.HasComponent<CPersistentCorpseItem>(illegalEntity))
                {
                    EntityManager.RemoveComponent<CPersistentCorpseItem>(illegalEntity);
                    if (EntityManager.HasComponent<CPreservedOvernight>(illegalEntity))
                    {
                        EntityManager.RemoveComponent<CPreservedOvernight>(illegalEntity);
                    }
                }

                if (EntityManager.HasComponent<CAppliance>(illegalEntity))
                {
                    if (persistentCorpsesActive && illegalSight.TurnIntoOnDayStart > 0)
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
            }

            ReconcileTemporaryHolderFlags(persistentCorpsesActive, holdersReceivingTemporaryPreservation);
        }

        private void HandleCorpseItem(Entity corpseEntity, bool persistentActive, HashSet<Entity> holdersReceivingTemporaryPreservation, List<Entity> holderBuffer)
        {
            if (persistentActive)
            {
                bool hadPreservedOvernight = EntityManager.HasComponent<CPreservedOvernight>(corpseEntity);
                if (!hadPreservedOvernight)
                {
                    EntityManager.AddComponentData(corpseEntity, new CPreservedOvernight());

                    if (!EntityManager.HasComponent<CPersistentCorpseItem>(corpseEntity))
                    {
                        EntityManager.AddComponent<CPersistentCorpseItem>(corpseEntity);
                    }
                }

                List<Entity> holders = CorpseStorageUtils.CollectHolderEntities(EntityManager, corpseEntity, holderBuffer);
                for (int h = 0; h < holders.Count; h++)
                {
                    Entity holderEntity = holders[h];
                    if (holderEntity == Entity.Null || !EntityManager.Exists(holderEntity))
                        continue;

                    if (!EntityManager.HasComponent<CAppliance>(holderEntity))
                        continue;

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
            else if (EntityManager.HasComponent<CPersistentCorpseItem>(corpseEntity))
            {
                EntityManager.RemoveComponent<CPersistentCorpseItem>(corpseEntity);
                if (EntityManager.HasComponent<CPreservedOvernight>(corpseEntity))
                {
                    EntityManager.RemoveComponent<CPreservedOvernight>(corpseEntity);
                }
            }
        }

        private bool IsCorpseItem(Entity entity)
        {
            if (!EntityManager.HasComponent<CItem>(entity))
            {
                return false;
            }

            CItem item = EntityManager.GetComponentData<CItem>(entity);
            return CorpseItemIDs.Contains(item.ID);
        }

        private static void EnsureCorpseIDCache()
        {
            if (CorpseItemIDs != null)
            {
                return;
            }

            CorpseItemIDs = new HashSet<int>
            {
                GDOUtils.GetCustomGameDataObject<CustomerCorpse>().ID,
                GDOUtils.GetCustomGameDataObject<RottenCustomerCorpse>().ID
            };
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
