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
            if (!ctx.Has<CIllegalSight>(entity) || !ctx.Has<CItem>(entity))
            {
                return;
            }

            CIllegalSight illegal = ctx.Get<CIllegalSight>(entity);
            if (illegal.TurnIntoOnDayStart <= 0)
            {
                return;
            }

            bool heldByPermanentPreserver = false;
            if (ctx.Has<CHeldBy>(entity))
            {
                Entity holderEntity = ctx.Get<CHeldBy>(entity).Holder;
                if (holderEntity != Entity.Null && ctx.Has<CPreservesContentsOvernight>(holderEntity))
                {
                    bool temporaryPreserver = false;
                    if (ctx.Has<CIllegalSightHolderPreserved>(holderEntity))
                    {
                        temporaryPreserver = ctx.Get<CIllegalSightHolderPreserved>(holderEntity).AddedPreserver;
                    }

                    heldByPermanentPreserver = !temporaryPreserver;
                }
            }

            if (heldByPermanentPreserver)
            {
                return;
            }

            QueueCorpseTransformation(ctx, entity, illegal);
        }

        private static void QueueCorpseTransformation(EntityContext ctx, Entity entity, CIllegalSight illegal)
        {
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
