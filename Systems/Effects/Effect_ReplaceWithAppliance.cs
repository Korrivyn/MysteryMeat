// Systems/Effects/Effect_ReplaceWithAppliance.cs
// Static helper that queues an appliance replacement for illegal sights so the swap can occur after the
// rotting fade completes at the start of the next day.

using Kitchen;
using KitchenData;
using KitchenMysteryMeat.Components;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems.Effects
{
    public static partial class CorpseEffects
    {
        public static void ReplaceWithAppliance(EntityContext ctx, Entity entity)
        {
            if (!ctx.Has<CIllegalSight>(entity))
                return;

            if (!ctx.Has<CAppliance>(entity) || !ctx.Has<CPosition>(entity))
                return;

            var illegal = ctx.Get<CIllegalSight>(entity);

            if (illegal.TurnIntoOnDayStart <= 0)
                return;

            if (ctx.Has<CPendingCorpseRot>(entity))
            {
                CPendingCorpseRot pending = ctx.Get<CPendingCorpseRot>(entity);

                if (pending.TargetApplianceID <= 0)
                {
                    pending.TargetApplianceID = illegal.TurnIntoOnDayStart;
                }

                if (pending.Duration <= 0f)
                {
                    pending.Duration = DefaultRotFadeDuration;
                }

                ctx.Set(entity, pending);
                return;
            }

            ctx.Set(entity, new CPendingCorpseRot
            {
                TargetApplianceID = illegal.TurnIntoOnDayStart,
                Duration = DefaultRotFadeDuration,
                Elapsed = 0f,
                PreservePortions = false
            });
        }
    }
}
