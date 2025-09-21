// Systems/Effects/Effect_TransformCorpse.cs
// Static helper: turns an entity that carries CIllegalSight into its configured TurnIntoOnDayStart item.
// Uses EntityContext (modern API already used in this project) and does not use KitchenData lookups.

using Kitchen;
using KitchenMysteryMeat.Components;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems.Effects
{
    public static partial class CorpseEffects
    {
        public static void TransformCorpse(EntityContext ctx, Entity entity)
        {
            // Confirm if this is a corpse & needs to be rotted.
            if (ctx.Has<CIllegalSight>(entity))
            {
                CIllegalSight illegalSight = ctx.Get<CIllegalSight>(entity);
                if (illegalSight.TurnIntoOnDayStart > 0)
                {
                    QueueCorpseTransformation(ctx, entity, illegalSight);
                }
            }
            // Otherwise, if it has an item, we can check for corpses within and rot accordingly.
            else if (ctx.Has<CItem>(entity))
            {
                Entity holderEntity = ctx.Get<CHeldBy>(entity).Holder;
                if (holderEntity != null && !ctx.Has<CPreservesContentsOvernight>(holderEntity))
                {
                    QueueCorpseTransformation(ctx, entity, ctx.Get<CIllegalSight>(holderEntity));
                }
            }
        }

        private static void QueueCorpseTransformation(EntityContext ctx, Entity entity, CIllegalSight illegalSight)
        {
            // Add the change marker (CChangeItemType) using the modern context API.
            ctx.Set(entity, new CChangeItemType { NewID = illegalSight.TurnIntoOnDayStart });

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
