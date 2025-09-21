// Systems/ApplyIllegalSightEffects.cs
// Invoker system: replaces the legacy StartOfDay/Overnight systems by invoking the new effect-style logic
// without using GameData or other legacy global lookups. This uses EntityContext (modern) which is used
// elsewhere in the project (see KillCustomers.cs).

using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Effects;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    public class ApplyIllegalSightEffects : StartOfDaySystem, IModSystem
    {
        private EntityQuery IllegalQuery;
        private EntityQuery ApplianceHolderQuery;
        private EntityQuery ApplianceStorageQuery;

        protected override void Initialise()
        {
            base.Initialise();

            IllegalQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CIllegalSight>() }
            });

            ApplianceHolderQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<CAppliance>(),
                    ComponentType.ReadOnly<CItemHolder>()
                }
            });

            ApplianceStorageQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<CAppliance>(),
                    ComponentType.ReadOnly<CItemStored>()
                }
            });
        }

        protected override void OnUpdate()
        {
            bool hasIllegalItems = !IllegalQuery.IsEmptyIgnoreFilter;
            bool hasHolders = !ApplianceHolderQuery.IsEmptyIgnoreFilter;
            bool hasStorage = !ApplianceStorageQuery.IsEmptyIgnoreFilter;

            if (!hasIllegalItems && !hasHolders && !hasStorage)
                return;

            // Create an EntityContext backed by the project's EntityManager
            EntityContext ctx = new EntityContext(EntityManager);
            HashSet<Entity> processedItems = new HashSet<Entity>();

            if (hasIllegalItems)
            {
                using NativeArray<Entity> illegals = IllegalQuery.ToEntityArray(Allocator.Temp);
                for (int i = illegals.Length - 1; i >= 0; --i)
                {
                    Entity e = illegals[i];

                    if (ctx.Has<CItem>(e))
                    {
                        if (processedItems.Add(e))
                        {
                            CorpseEffects.TransformCorpse(ctx, e);
                        }
                    }
                    else if (ctx.Has<CAppliance>(e))
                    {
                        ProcessApplianceSurfaceContents(ctx, e, processedItems);
                        CorpseEffects.ReplaceWithAppliance(ctx, e);
                    }
                }
            }

            if (hasHolders)
            {
                ProcessAllItemHolders(ctx, processedItems);
            }

            if (hasStorage)
            {
                ProcessAllStoredItems(ctx, processedItems);
            }
        }

        private void ProcessAllItemHolders(EntityContext ctx, HashSet<Entity> processedItems)
        {
            using NativeArray<Entity> holders = ApplianceHolderQuery.ToEntityArray(Allocator.Temp);
            for (int i = holders.Length - 1; i >= 0; --i)
            {
                Entity appliance = holders[i];
                CItemHolder holder = EntityManager.GetComponentData<CItemHolder>(appliance);
                TryTransformHeldEntity(ctx, holder.HeldItem, processedItems);
            }
        }

        private void ProcessAllStoredItems(EntityContext ctx, HashSet<Entity> processedItems)
        {
            using NativeArray<Entity> storages = ApplianceStorageQuery.ToEntityArray(Allocator.Temp);
            for (int i = storages.Length - 1; i >= 0; --i)
            {
                Entity appliance = storages[i];
                DynamicBuffer<CItemStored> storedItems = EntityManager.GetBuffer<CItemStored>(appliance);
                for (int j = 0; j < storedItems.Length; j++)
                {
                    TryTransformHeldEntity(ctx, storedItems[j].StoredItem, processedItems);
                }
            }
        }

        private void ProcessApplianceSurfaceContents(EntityContext ctx, Entity appliance, HashSet<Entity> processedItems)
        {
            if (processedItems == null)
                return;

            if (EntityManager.HasComponent<CItemHolder>(appliance))
            {
                CItemHolder holder = EntityManager.GetComponentData<CItemHolder>(appliance);
                TryTransformHeldEntity(ctx, holder.HeldItem, processedItems);
            }

            if (EntityManager.HasComponent<CItemStored>(appliance))
            {
                DynamicBuffer<CItemStored> storedItems = EntityManager.GetBuffer<CItemStored>(appliance);
                for (int i = 0; i < storedItems.Length; i++)
                {
                    TryTransformHeldEntity(ctx, storedItems[i].StoredItem, processedItems);
                }
            }
        }

        private void TryTransformHeldEntity(EntityContext ctx, Entity heldItem, HashSet<Entity> processedItems)
        {
            if (heldItem == Entity.Null || !EntityManager.Exists(heldItem))
                return;

            if (!ctx.Has<CIllegalSight>(heldItem))
                return;

            if (!processedItems.Add(heldItem))
                return;

            CorpseEffects.TransformCorpse(ctx, heldItem);
        }
    }
}
