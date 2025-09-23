// Systems/Effects/Effect_TransformCorpse.cs
// Static helper: turns an entity that carries CIllegalSight into its configured TurnIntoOnDayStart item.
// Uses EntityContext (modern API already used in this project) and does not use KitchenData lookups.

using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMysteryMeat;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Items;
using KitchenMysteryMeat.Systems;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems.Effects
{
    /// <summary>
    /// Provides corpse effect helpers that transform illegal items into their rotten counterparts.
    /// </summary>
    public static partial class CorpseEffects
    {
        // Holds cached corpse mapping IDs so lookups can be reused without repeated GDO queries.
        private static readonly int CustomerCorpseID = GDOUtils.GetCustomGameDataObject<CustomerCorpse>().ID;

        // Stores the rotten corpse ID used as the transformation target when fresh corpse data is missing.
        private static readonly int RottenCustomerCorpseID = GDOUtils.GetCustomGameDataObject<RottenCustomerCorpse>().ID;

        /// <summary>
        /// Handles illegal corpse item transformation while respecting preservers and logging detailed diagnostics.
        /// </summary>
        public static void TransformCorpse(EntityContext ctx, Entity entity)
        {
            // Guard: ensure the entity is an illegal sight item before proceeding.
            if (!ctx.Has<CIllegalSight>(entity) || !ctx.Has<CItem>(entity))
            {
                LogCorpseDebug($"[TransformCorpse] Entity {entity.Index} skipped - missing illegal sight or item component.");
                return;
            }

            CIllegalSight illegal = ctx.Get<CIllegalSight>(entity);

            // Guard: confirm the illegal sight provides a valid rotten replacement or recover it from the blueprint when stale.
            if (illegal.TurnIntoOnDayStart <= 0)
            {
                // Attempt to recover the rotten target from the source item blueprint.
                CItem itemData = ctx.Get<CItem>(entity);
                bool recoveredFromBlueprint = false;

                // Resolve the blueprint so we can inspect its attached properties.
                if (GameData.Main.TryGet(itemData.ID, out Item itemBlueprint, false))
                {
                    // Inspect the blueprint properties for a CIllegalSight definition with a valid transformation target.
                    if (itemBlueprint?.Properties != null)
                    {
                        // Iterate through the blueprint properties to locate a valid illegal sight definition.
                        foreach (IItemProperty property in itemBlueprint.Properties)
                        {
                            if (property is CIllegalSight blueprintIllegal && blueprintIllegal.TurnIntoOnDayStart > 0)
                            {
                                illegal.TurnIntoOnDayStart = blueprintIllegal.TurnIntoOnDayStart;
                                ctx.Set(entity, illegal);
                                recoveredFromBlueprint = true;
                                LogCorpseDebug($"[TransformCorpse] Entity {entity.Index} recovered TurnIntoOnDayStart {illegal.TurnIntoOnDayStart} from blueprint {itemData.ID}.");
                                break;
                            }
                        }
                    }

                    if (!recoveredFromBlueprint)
                    {
                        // Log the absence of a usable illegal sight property when inspection succeeds.
                        LogCorpseDebug($"[TransformCorpse] Entity {entity.Index} item blueprint {itemData.ID} lacked a valid TurnIntoOnDayStart.");
                    }
                }
                else
                {
                    // Log the inability to resolve the item blueprint so missing data can be traced.
                    LogCorpseDebug($"[TransformCorpse] Entity {entity.Index} item blueprint {itemData.ID} could not be resolved for TurnIntoOnDayStart recovery.");
                }

                // Attempt to resolve the rotten target through known corpse mappings when the blueprint lacks data.
                if (!recoveredFromBlueprint && TryResolveKnownCorpseMapping(itemData.ID, out int mappedRottenID))
                {
                    illegal.TurnIntoOnDayStart = mappedRottenID;
                    ctx.Set(entity, illegal);
                    recoveredFromBlueprint = true;
                    LogCorpseDebug($"[TransformCorpse] Entity {entity.Index} mapped fresh corpse {itemData.ID} to rotten {mappedRottenID} via known defaults.");
                }

                // Skip further handling when the item already represents the rotten corpse blueprint.
                if (!recoveredFromBlueprint && itemData.ID == RottenCustomerCorpseID)
                {
                    LogCorpseDebug($"[TransformCorpse] Entity {entity.Index} already uses rotten corpse blueprint {itemData.ID}; decay skipped.");
                    return;
                }

                if (!recoveredFromBlueprint)
                {
                    // Abort when no valid rotten target is discovered after the fallback recovery attempt.
                    LogCorpseDebug($"[TransformCorpse] Entity {entity.Index} skipped - no TurnIntoOnDayStart configured.");
                    return;
                }
            }

            LogCorpseDebug($"[TransformCorpse] Preparing to rot entity {entity.Index} into item {illegal.TurnIntoOnDayStart}.");

            bool isHeld = ctx.Has<CHeldBy>(entity);
            CHeldBy heldData = default;
            Entity holderEntity = Entity.Null;

            // Capture holder data so we can reattach the rotten replacement correctly.
            if (isHeld)
            {
                heldData = ctx.Get<CHeldBy>(entity);
                holderEntity = heldData.Holder;
                LogCorpseDebug($"[TransformCorpse] Entity {entity.Index} is held by {holderEntity.Index}.");
            }

            bool skipForPreserver = false;

            // Detect genuine preservers so we avoid rotting items stored in freezers or similar appliances.
            if (holderEntity != Entity.Null && ctx.Has<CPreservesContentsOvernight>(holderEntity))
            {
                bool hadTemporaryPreserver = ctx.Has<CIllegalSightHolderPreserved>(holderEntity) && ctx.Get<CIllegalSightHolderPreserved>(holderEntity).AddedPreserver;
                skipForPreserver = !hadTemporaryPreserver;

                if (skipForPreserver)
                {
                    LogCorpseDebug($"[TransformCorpse] Holder {holderEntity.Index} genuinely preserves contents - decay skipped.");
                }
                else
                {
                    LogCorpseDebug($"[TransformCorpse] Holder {holderEntity.Index} only has a temporary preserver - decay allowed.");
                }
            }

            // Guard: respect genuine preservers even after logging the details.
            if (skipForPreserver)
            {
                return;
            }

            bool hasSplitComponent = ctx.Has<CSplittableItem>(entity);
            int remainingCount = 0;
            int totalCount = 0;

            // Preserve portion counts so the rotten corpse inherits the same servings.
            if (hasSplitComponent)
            {
                CSplittableItem split = ctx.Get<CSplittableItem>(entity);
                remainingCount = split.RemainingCount;
                totalCount = split.TotalCount;
                LogCorpseDebug($"[TransformCorpse] Entity {entity.Index} portions captured ({remainingCount}/{totalCount}).");
            }

            Entity newCorpse = ctx.CreateEntity();
            ctx.Set(newCorpse, new CCreateItem
            {
                ID = illegal.TurnIntoOnDayStart
            });
            LogCorpseDebug($"[TransformCorpse] Spawned rotten corpse entity {newCorpse.Index}.");

            // Retain portion data if applicable.
            if (hasSplitComponent)
            {
                ctx.Set(newCorpse, new CPersistPortions
                {
                    RemainingCount = remainingCount,
                    TotalCount = totalCount
                });
                LogCorpseDebug($"[TransformCorpse] Applied CPersistPortions to rotten corpse {newCorpse.Index}.");
            }

            bool hasPosition = ctx.Has<CPosition>(entity);

            // Reattach the new corpse to its previous holder or position.
            if (holderEntity != Entity.Null)
            {
                ctx.Set(newCorpse, heldData);

                // Update the holder so it now references the rotten corpse entity.
                if (ctx.Has<CItemHolder>(holderEntity))
                {
                    CItemHolder holderData = ctx.Get<CItemHolder>(holderEntity);
                    holderData.HeldItem = newCorpse;
                    ctx.Set(holderEntity, holderData);
                    LogCorpseDebug($"[TransformCorpse] Updated holder {holderEntity.Index} to reference rotten corpse {newCorpse.Index}.");
                }
            }
            // Assign the world position when the corpse was resting on the ground.
            else if (hasPosition)
            {
                ctx.Set(newCorpse, ctx.Get<CPosition>(entity));
                LogCorpseDebug($"[TransformCorpse] Assigned world position to rotten corpse {newCorpse.Index}.");
            }

            // Remove the original corpse item so only the rotten replacement remains.
            ctx.Destroy(entity);
            LogCorpseDebug($"[TransformCorpse] Destroyed original entity {entity.Index} after spawning rotten corpse {newCorpse.Index}.");
        }

        /// <summary>
        /// Attempts to resolve a rotten corpse ID for known fresh corpse blueprints.
        /// </summary>
        private static bool TryResolveKnownCorpseMapping(int itemID, out int rottenID)
        {
            // Default the rotten ID before attempting to match against known corpse templates.
            rottenID = 0;

            // Match the incoming item ID with the fresh corpse blueprint to recover its rotten successor.
            if (itemID == CustomerCorpseID)
            {
                rottenID = RottenCustomerCorpseID;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Emits debug output through the shared debug logging helper so verbosity remains configurable.
        /// </summary>
        private static void LogCorpseDebug(string message)
        {
            DebugLogSystem.LogVerbose(message);
        }
    }
}
