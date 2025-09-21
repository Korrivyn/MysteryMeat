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
            if (!ctx.Has<CIllegalSight>(entity))
                return;

            if (!ctx.Has<CItem>(entity))
                return;

            CIllegalSight illegal = ctx.Get<CIllegalSight>(entity);

            if (illegal.TurnIntoOnDayStart <= 0)
                return;

            Entity preservingHolder = Entity.Null;

            if (ctx.Has<CHeldBy>(entity))
            {
                CHeldBy holder = ctx.Get<CHeldBy>(entity);
                if (holder.Holder != Entity.Null && ctx.Has<CPreservesContentsOvernight>(holder.Holder))
                {
                    preservingHolder = holder.Holder;
                }
            }

            bool holderPreserverAddedByMod = false;

            if (preservingHolder != Entity.Null)
            {
                if (ctx.Has<CIllegalSightHolderPreserved>(preservingHolder))
                {
                    var marker = ctx.Get<CIllegalSightHolderPreserved>(preservingHolder);
                    holderPreserverAddedByMod = marker.AddedPreserver;
                }
            }

            if (holderPreserverAddedByMod)
            {
                // Temporarily remove the preserver so the overnight transformation can proceed.
                ctx.Remove<CPreservesContentsOvernight>(preservingHolder);
            }

            if (ctx.Has<CPreservedOvernight>(entity))
            {
                // Explicit preservation marks can block the swap as well, so clear them before
                // applying the change. The replacement item will reapply its own properties.
                ctx.Remove<CPreservedOvernight>(entity);
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
