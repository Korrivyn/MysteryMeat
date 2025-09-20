// Systems/Effects/Effect_TransformCorpse.cs
// Static helper: queues the conversion of an illegal corpse into its TurnIntoOnDayStart item so the
// actual swap can occur after the overnight phase, allowing the visual to fade during the next day.
// Uses EntityContext (modern API already used in this project) and does not use KitchenData lookups.

using Kitchen;
using KitchenMysteryMeat.Components;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems.Effects
{
    public static partial class CorpseEffects
    {
        internal const float DefaultRotFadeDuration = 1.5f;

        public static void TransformCorpse(EntityContext ctx, Entity entity)
        {
            if (!ctx.Has<CIllegalSight>(entity))
                return;

            if (!ctx.Has<CItem>(entity))
                return;

            CIllegalSight illegal = ctx.Get<CIllegalSight>(entity);

            if (illegal.TurnIntoOnDayStart <= 0)
                return;

            // Queue the rot so the visual can fade into the rotten variant at the start of day.
            if (ctx.Has<CPendingCorpseRot>(entity))
            {
                CPendingCorpseRot pending = ctx.Get<CPendingCorpseRot>(entity);

                if (pending.TargetItemID <= 0)
                {
                    pending.TargetItemID = illegal.TurnIntoOnDayStart;
                }

                if (pending.Duration <= 0f)
                {
                    pending.Duration = DefaultRotFadeDuration;
                }

                if (!pending.PreservePortions && ctx.Has<CSplittableItem>(entity))
                {
                    pending.PreservePortions = true;
                }

                ctx.Set(entity, pending);
                return;
            }

            ctx.Set(entity, new CPendingCorpseRot
            {
                TargetItemID = illegal.TurnIntoOnDayStart,
                Duration = DefaultRotFadeDuration,
                Elapsed = 0f,
                PreservePortions = ctx.Has<CSplittableItem>(entity)
            });
        }
    }
}
