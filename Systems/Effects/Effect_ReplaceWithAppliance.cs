// Systems/Effects/Effect_ReplaceWithAppliance.cs
// Effect that replaces the current entity with the appliance ID stored in the entity's CIllegalSight.
// Useful for overnight replacements.

using Kitchen;
using KitchenMods;
using KitchenLib.Customs;
using KitchenMysteryMeat.Components;
using System.Reflection;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems.Effects
{
    public class Effect_ReplaceWithAppliance : Effect, IEffect
    {
        public override void Apply(EntityContext ctx, Entity entity)
        {
            if (!ctx.Has<CIllegalSight>(entity))
                return;

            var illegal = ctx.Get<CIllegalSight>(entity);

            if (!ctx.Has<CPosition>(entity))
                return;

            var pos = ctx.Get<CPosition>(entity);

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