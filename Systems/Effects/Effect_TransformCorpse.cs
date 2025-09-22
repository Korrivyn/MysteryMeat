// Systems/Effects/Effect_TransformCorpse.cs
// Static helper: turns an entity that carries CIllegalSight into its configured TurnIntoOnDayStart item.
// Uses EntityContext (modern API already used in this project) and does not use KitchenData lookups.

using Kitchen;
using KitchenData;
using KitchenMysteryMeat.Components;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems.Effects
{
    public static partial class CorpseEffects
    {
        // Coordinates the corpse transformation process for any entity flagged with illegal sight.
        public static void TransformCorpse(EntityContext ctx, Entity entity)
        {
            // Evaluate whether the entity is an item so we can follow the item transformation path.
            if (ctx.Has<CItem>(entity))
            {
                // Route held corpses through the held-item logic to respect preserving containers.
                if (ctx.Has<CHeldBy>(entity))
                {
                    QueueHeldCorpseTransformation(ctx, entity);
                }
                else
                {
                    // Process standalone corpse items that should decay in place.
                    QueueCorpseTransformation(ctx, entity);
                }
            }
            // Delegate to appliance replacement when the illegal entity is an appliance instead of an item.
            else if (ctx.Has<CAppliance>(entity))
            {
                // Replace appliance corpses that exist as map objects rather than items.
                ReplaceApplianceCorpses(ctx, entity);
            }
        }

        // Handles corpse items that are stored by another entity so they can decay correctly.
        private static void QueueHeldCorpseTransformation(EntityContext ctx, Entity entity)
        {
            // Retrieve the current holder so we know where the corpse is stored.
            CHeldBy heldBy = ctx.Get<CHeldBy>(entity);
            Entity holderEntity = heldBy.Holder;

            // Determine whether the holder has an active overnight preserver component.
            bool holderHasOvernightPreserver = holderEntity != Entity.Null && ctx.Has<CPreservesContentsOvernight>(holderEntity);

            // Capture whether the preserver was injected solely for illegal-sight handling.
            bool holderPreserverIsTemporary = holderHasOvernightPreserver && ctx.Has<CIllegalSightHolderPreserved>(holderEntity) && ctx.Get<CIllegalSightHolderPreserved>(holderEntity).AddedPreserver;

            // Determine whether the corpse should decay while being held, ignoring temporary preservers.
            bool shouldTransformWhileHeld = holderEntity == Entity.Null || !holderHasOvernightPreserver || holderPreserverIsTemporary;

            // Queue the standard transformation when the holder cannot preserve its contents or the preserver is temporary.
            if (shouldTransformWhileHeld)
            {
                QueueCorpseTransformation(ctx, entity);
            }
        }

        // Queues the default corpse transformation on entities that can simply change their item type.
        private static void QueueCorpseTransformation(EntityContext ctx, Entity entity)
        {
            CIllegalSight illegalSight = ctx.Get<CIllegalSight>(entity);
            // Confirm this corpse is ready to be decayed.
            if (illegalSight.TurnIntoOnDayStart > 0)
            {
                // Add the change marker (CChangeItemType) using the modern context API.
                ctx.Set(entity, new CChangeItemType { NewID = illegalSight.TurnIntoOnDayStart });

                // Preserve portions if splittable
                if (ctx.Has<CSplittableItem>(entity))
                {
                    CSplittableItem split = ctx.Get<CSplittableItem>(entity);
                    ctx.Set(entity, new CPersistPortions
                    {
                        RemainingCount = split.RemainingCount,
                        TotalCount = split.TotalCount
                    });
                }
            }
        }

        // Replaces corpse appliances placed directly in the world with their rotten variants.
        private static void ReplaceApplianceCorpses(EntityContext ctx, Entity entity)
        {
            CIllegalSight illegal = ctx.Get<CIllegalSight>(entity);
            CPosition pos = ctx.Get<CPosition>(entity);

            // Only attempt to replace the appliance if the data specifies a rotten version.
            if (illegal.TurnIntoOnDayStart > 0)
            {

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
}
