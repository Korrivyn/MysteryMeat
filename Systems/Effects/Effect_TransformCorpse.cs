// Systems/Effects/Effect_TransformCorpse.cs
// Effect-style transform: turns an entity that carries CIllegalSight into its configured TurnIntoOnDayStart item.
// Uses EntityContext (modern API already used in this project) and does not use KitchenData lookups.

using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using System.Reflection;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems.Effects
{
    public class Effect_TransformCorpse : Effect, IEffect
    {
        // Apply is called with an EntityContext by the invoker system (below).
        public override void Apply(EntityContext ctx, Entity entity)
        {
            // Only act if the entity actually has CIllegalSight (the data-driven TurnIntoOnDayStart)
            if (!ctx.Has<CIllegalSight>(entity))
                return;

            CIllegalSight illegal = ctx.Get<CIllegalSight>(entity);

            // ITEM CASE: if entity is an item, attempt to queue a change
            if (ctx.Has<CItem>(entity))
            {
                // If item is held, ensure the holder does not preserve contents overnight.
                if (ctx.Has<CHeldBy>(entity))
                {
                    CHeldBy holder = ctx.Get<CHeldBy>(entity);
                    if (holder.Holder != Entity.Null && ctx.Has<CPreservesContentsOvernight>(holder.Holder))
                    {
                        // Holder preserves contents — do nothing.
                        return;
                    }
                }

                // Add the change marker (CChangeItemType) using the modern context API.
                ctx.Set(entity, new CChangeItemType { NewID = illegal.TurnIntoOnDayStart });

                // Preserve portions if splittable
                if (ctx.Has<CSplittableItem>(entity))
                {
                    var split = ctx.Get<CSplittableItem>(entity);
                    ctx.Set(entity, new CPersistPortions
                    {
                        RemainingCount = split.RemainingCount,
                        TotalCount = split.TotalCount
                    });
                }

                return;
            }

            // APPLIANCE CASE: spawn a new appliance and remove the old one
            if (ctx.Has<CAppliance>(entity) && ctx.Has<CPosition>(entity))
            {
                var pos = ctx.Get<CPosition>(entity);

                // Create a new appliance entity and mark it for creation by using CCreateAppliance + position
                Entity newAppliance = ctx.CreateEntity();
                ctx.Set(newAppliance, new CCreateAppliance
                {
                    ID = illegal.TurnIntoOnDayStart,
                    ForceLayer = OccupancyLayer.Ceiling
                });
                ctx.Set(newAppliance, pos);

                // Destroy the original entity
                ctx.Destroy(entity);
            }
        }
    }
}