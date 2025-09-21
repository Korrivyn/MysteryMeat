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
        protected override void OnUpdate()
        {
            // Build query of illegal entities
            var query = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CIllegalSight>() }
            });

            using (NativeArray<Entity> illegals = query.ToEntityArray(Allocator.Temp))
            {
                if (illegals.Length == 0)
                    return;

                // Create an EntityContext backed by the project's EntityManager
                EntityContext ctx = new EntityContext(EntityManager);
                HashSet<Entity> processedItems = new HashSet<Entity>();

                // Ensure we catch corpses sitting on counters or stored overnight before
                // we mutate any appliances. This sweeps every holder/storage entity so we
                // do not rely on the illegal query hitting the held item directly.
                ProcessGlobalIllegalHolders(ctx, processedItems);

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

        private void ProcessGlobalIllegalHolders(EntityContext ctx, HashSet<Entity> processedItems)
        {
            if (processedItems == null)
                return;

            var holderQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CItemHolder>() }
            });

            using (NativeArray<Entity> holders = holderQuery.ToEntityArray(Allocator.Temp))
            using (NativeArray<CItemHolder> holderData = holderQuery.ToComponentDataArray<CItemHolder>(Allocator.Temp))
            {
                for (int i = holders.Length - 1; i >= 0; --i)
                {
                    TryTransformHeldEntity(ctx, holderData[i].HeldItem, processedItems);
                }
            }

            var storageQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CItemStored>() }
            });

            using (NativeArray<Entity> storages = storageQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = storages.Length - 1; i >= 0; --i)
                {
                    DynamicBuffer<CItemStored> storedItems = EntityManager.GetBuffer<CItemStored>(storages[i]);
                    for (int j = 0; j < storedItems.Length; ++j)
                    {
                        TryTransformHeldEntity(ctx, storedItems[j].StoredItem, processedItems);
                    }
                }
            }
        }
    }
}
