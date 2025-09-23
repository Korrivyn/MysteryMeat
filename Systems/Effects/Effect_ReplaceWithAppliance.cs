// Systems/Effects/Effect_ReplaceWithAppliance.cs
// Static helper that replaces the current entity with the appliance ID stored in the entity's CIllegalSight.
// Useful for overnight replacements.

using Kitchen;
using KitchenData;
using KitchenMysteryMeat;
using KitchenMysteryMeat.Components;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems.Effects
{
    public static partial class CorpseEffects
    {
        /// <summary>
        /// Replaces an illegal sight appliance with its configured overnight appliance while logging diagnostics.
        /// </summary>
        /// <param name="ctx">Entity context used to query and modify game state.</param>
        /// <param name="entity">Entity flagged with illegal sight data to replace.</param>
        public static void ReplaceWithAppliance(EntityContext ctx, Entity entity)
        {
            // Guard: ensure the appliance is flagged as an illegal sight before transforming it.
            if (!ctx.Has<CIllegalSight>(entity))
            {
                LogCorpseDebug($"[ReplaceWithAppliance] Entity {entity.Index} skipped - missing illegal sight component.");
                return;
            }

            // Guard: verify appliance and position data exist so we can spawn the replacement correctly.
            if (!ctx.Has<CAppliance>(entity) || !ctx.Has<CPosition>(entity))
            {
                LogCorpseDebug($"[ReplaceWithAppliance] Entity {entity.Index} skipped - missing appliance or position component.");
                return;
            }

            CIllegalSight illegal = ctx.Get<CIllegalSight>(entity);
            CPosition pos = ctx.Get<CPosition>(entity);

            // Guard: ensure a valid replacement appliance ID is specified.
            if (illegal.TurnIntoOnDayStart <= 0)
            {
                LogCorpseDebug($"[ReplaceWithAppliance] Entity {entity.Index} skipped - no TurnIntoOnDayStart configured.");
                return;
            }

            LogCorpseDebug($"[ReplaceWithAppliance] Replacing appliance entity {entity.Index} with {illegal.TurnIntoOnDayStart}.");

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
            LogCorpseDebug($"[ReplaceWithAppliance] Destroyed original appliance entity {entity.Index} after spawning {illegal.TurnIntoOnDayStart}.");
        }
    }
}
