// Systems/Effects/Effect_ReplaceWithAppliance.cs
// Static helper that replaces the current entity with the appliance ID stored in the entity's CIllegalSight.
// Useful for overnight replacements.

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
            var pos = ctx.Get<CPosition>(entity);

            if (illegal.TurnIntoOnDayStart <= 0)
                return;

            // Create new appliance entity
            Entity newEntity = ctx.CreateEntity();
            ctx.Set(newEntity, new CCreateAppliance
            {
                ID = illegal.TurnIntoOnDayStart,
                ForceLayer = OccupancyLayer.Ceiling
            });
            ctx.Set(newEntity, pos);

            // Destroy the original
            ctx.Destroy(entity);
        }
    }
}
