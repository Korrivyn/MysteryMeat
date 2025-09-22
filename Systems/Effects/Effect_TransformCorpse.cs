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
        // Entrypoint invoked for every entity that carries the illegal sight marker overnight.
        public static void TransformCorpse(EntityContext ctx, Entity entity)
        {
            // If this is a corpse on an appliance
            if (ctx.Has<CHeldBy>(entity))
            {
                QueueUnpreservedCorpses(ctx, entity);
            }
            else
            {
                // The Corpse Item itself
                if (ctx.Has<CItem>(entity))
                {
                    QueueCorpseTransformation(ctx, entity);
                }
                else // Corpse on the ground
                {
                    ReplaceApplianceCorpses(ctx, entity);
                }
            }
        }

        // Handles corpses that are held inside another entity, such as counters or containers.
        private static void QueueUnpreservedCorpses(EntityContext ctx, Entity entity)
        {
            Entity holderEntity = ctx.Get<CHeldBy>(entity).Holder;
            if (holderEntity != Entity.Null)
            {
                // Evaluate whether this holder is allowed to prevent corpses from rotting.
                if (HolderBlocksCorpseTransformation(ctx, holderEntity))
                {
                    return;
                }

                QueueCorpseTransformation(ctx, entity); // In a container, but it's not a preserving one.
            }
            else
            {
                QueueCorpseTransformation(ctx, entity); // OR Not in any container at all.
            }
        }

        // Determines whether the holder's overnight preservation status should block corpse decay.
        private static bool HolderBlocksCorpseTransformation(EntityContext ctx, Entity holderEntity)
        {
            // Skip early if the holder does not preserve contents overnight.
            if (!ctx.Has<CPreservesContentsOvernight>(holderEntity))
            {
                return false;
            }

            // Ignore preservation added by the Persistent Corpses status so corpses still rot.
            if (ctx.Has<CIllegalSightHolderPreserved>(holderEntity))
            {
                CIllegalSightHolderPreserved marker = ctx.Get<CIllegalSightHolderPreserved>(holderEntity);

                // Only block rot when the holder originally preserved contents before our override.
                if (marker.AddedPreserver)
                {
                    return false;
                }
            }

            return true;
        }


        // Queues the corpse entity to transform into its rotten counterpart on the next frame.
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

        // Replaces corpse appliances with their rotten versions when no item component exists.
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
