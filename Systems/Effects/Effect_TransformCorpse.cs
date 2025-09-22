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
        // Coordinates the corpse transformation process for any entity flagged with illegal sight.
        public static void TransformCorpse(EntityContext ctx, Entity entity)
        {
            // Handle corpses that are currently held by another entity (for example, a counter).
            if (ctx.Has<CHeldBy>(entity))
            {
                QueueUnpreservedCorpses(ctx, entity);
            }
            else
            {
                // Process standalone corpse items that should decay in place.
                if (ctx.Has<CItem>(entity))
                {
                    QueueCorpseTransformation(ctx, entity);
                }
                else
                {
                    // Replace appliance corpses that exist as map objects rather than items.
                    ReplaceApplianceCorpses(ctx, entity);
                }
            }
        }

        // Handles corpse items that are stored by another entity so they can decay correctly.
        private static void QueueUnpreservedCorpses(EntityContext ctx, Entity entity)
        {
            // Retrieve the current holder so we know where the corpse is stored.
            CHeldBy heldBy = ctx.Get<CHeldBy>(entity);
            Entity holderEntity = heldBy.Holder;

            // If there is no holder we can fall back to the standard item transformation behaviour.
            if (holderEntity == Entity.Null)
            {
                QueueCorpseTransformation(ctx, entity);
            }
            else
            {
                // Respect containers that preserve contents overnight by leaving the corpse untouched.
                if (!ctx.Has<CPreservesContentsOvernight>(holderEntity))
                {
                    SwapCorpseWithRottenVersion(ctx, entity, holderEntity, heldBy);
                }
            }
        }

        // Creates a rotten corpse item while keeping the existing portion count and swaps it into the holder.
        private static void SwapCorpseWithRottenVersion(EntityContext ctx, Entity corpseEntity, Entity holderEntity, CHeldBy heldBy)
        {
            // Determine what the corpse should become when it rots.
            CIllegalSight illegalSight = ctx.Get<CIllegalSight>(corpseEntity);
            int rottenCorpseID = illegalSight.TurnIntoOnDayStart;

            // Only perform the swap if we have a valid rotten corpse to create.
            if (rottenCorpseID <= 0)
            {
                return;
            }

            // Track how many portions remain so the new corpse can inherit the same amount.
            CPersistPortions persistPortions = default;
            bool hasPersistingPortions = false;
            if (ctx.Has<CSplittableItem>(corpseEntity))
            {
                CSplittableItem split = ctx.Get<CSplittableItem>(corpseEntity);
                persistPortions = new CPersistPortions
                {
                    RemainingCount = split.RemainingCount,
                    TotalCount = split.TotalCount
                };
                hasPersistingPortions = true;
            }

            // Create a new entity that will become the rotten corpse held by the same container.
            Entity rottenCorpseEntity = ctx.CreateEntity();
            ctx.Set(rottenCorpseEntity, new CCreateItem
            {
                ID = rottenCorpseID,
                Holder = holderEntity
            });
            ctx.Set(rottenCorpseEntity, heldBy);

            // Preserve splittable data so partially used corpses remain partially used after rotting.
            if (hasPersistingPortions)
            {
                ctx.Set(rottenCorpseEntity, persistPortions);
            }

            // Ensure the holder now references the rotten corpse instead of the fresh corpse.
            if (ctx.Has<CItemHolder>(holderEntity))
            {
                CItemHolder itemHolder = ctx.Get<CItemHolder>(holderEntity);
                // Only swap the reference when the holder is still pointing at the original corpse.
                if (itemHolder.HeldItem == corpseEntity)
                {
                    itemHolder.HeldItem = rottenCorpseEntity;
                    ctx.Set(holderEntity, itemHolder);
                }
            }

            // Remove the original corpse because the rotten replacement has taken its place.
            ctx.Destroy(corpseEntity);
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
