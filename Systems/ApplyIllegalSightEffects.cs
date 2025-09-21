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

            if (EntityManager.HasBuffer<CItemStored>(appliance))
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

            if (!processedItems.Add(heldItem))
                return;

            CorpseEffects.TransformCorpse(ctx, heldItem);
        }
    }
}
