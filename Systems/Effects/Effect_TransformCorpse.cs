// Systems/Effects/Effect_TransformCorpse.cs
// Static helper: turns an entity that carries CIllegalSight into its configured TurnIntoOnDayStart item.
// Uses EntityContext (modern API already used in this project) and does not use KitchenData lookups.

using Kitchen;
using KitchenData;
using KitchenMysteryMeat.Components;
using Unity.Entities;
using static UnityEngine.EventSystems.EventTrigger;

namespace KitchenMysteryMeat.Systems.Effects
{
    public static partial class CorpseEffects
    {
        public static void TransformCorpse(EntityContext ctx, Entity entity)
        {
            // Any item ready to be transformed.
            if (ctx.Has<CItem>(entity))
            {
                // If it's a corpse item on an appliance
                if (ctx.Has<CAppliance>(entity))
                {
                    /// TODO: Get the corpse entity from the appliance.
                    // Remove from the appliance, run through TransformCorpse again.
                    TransformCorpse(ctx, entity);
                }
                QueueUnpreservedCorpses(ctx, entity);
            }
            else // Corpse Appliance on the floor.
            if (ctx.Has<CIllegalSight>(entity))
            {
                ReplaceApplianceCorpses(ctx, entity);
            }
        }

        private static void QueueUnpreservedCorpses (EntityContext ctx, Entity entity)
        {
            // If this is a corpse specifically.
            if (ctx.Has<CIllegalSight>(entity))
            {
                CIllegalSight illegalSight = ctx.Get<CIllegalSight>(entity);
                Entity holderEntity = ctx.Get<CHeldBy>(entity).Holder;
                if (holderEntity != Entity.Null)
                {
                    if (!ctx.Has<CPreservesContentsOvernight>(holderEntity))
                    {
                        QueueCorpseTransformation(ctx, entity, illegalSight); // In a container, but it's not a preserving one.
                    }
                }
                else
                {
                    QueueCorpseTransformation(ctx, entity, illegalSight); // OR Not in any container at all.
                }
            }
        }


        private static void QueueCorpseTransformation(EntityContext ctx, Entity entity, CIllegalSight illegalSight)
        {
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

        private static void ReplaceApplianceCorpses(EntityContext ctx, Entity entity)
        {
            CIllegalSight illegal = ctx.Get<CIllegalSight>(entity);
            CPosition pos = ctx.Get<CPosition>(entity);

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
