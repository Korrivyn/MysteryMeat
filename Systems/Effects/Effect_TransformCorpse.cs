// Systems/Effects/Effect_TransformCorpse.cs
// Static helper: turns an entity that carries CIllegalSight into its configured TurnIntoOnDayStart item.
// Uses EntityContext (modern API already used in this project) and does not use KitchenData lookups.

using Kitchen;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems;
using System.Collections.Generic;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems.Effects
{
    public static partial class CorpseEffects
    {
        public static void TransformCorpse(EntityContext ctx, Entity entity)
        {
            if (!ctx.Has<CIllegalSight>(entity))
                return;

            if (!ctx.Has<CItem>(entity))
                return;

            CIllegalSight illegal = ctx.Get<CIllegalSight>(entity);

            if (illegal.TurnIntoOnDayStart <= 0)
                return;

            // If the item is stored in a naturally preserving appliance, skip the transform.
            List<Entity> holders = CorpseStorageUtils.CollectHolderEntities(ctx, entity, null);
            for (int i = 0; i < holders.Count; i++)
            {
                Entity holder = holders[i];
                if (holder == Entity.Null)
                {
                    continue;
                }

                if (!ctx.Has<CPreservesContentsOvernight>(holder))
                {
                    continue;
                }

                if (!ctx.Has<CPersistentCorpseHolder>(holder))
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
        }
    }
}
