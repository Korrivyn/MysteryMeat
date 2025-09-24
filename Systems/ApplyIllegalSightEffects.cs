// Systems/ApplyIllegalSightEffects.cs
// Invoker system: replaces the legacy StartOfDay/Overnight systems by invoking the new effect-style logic
// without using GameData or other legacy global lookups. This uses EntityContext (modern) which is used
// elsewhere in the project (see KillCustomers.cs).

using Kitchen;
using KitchenMods;
using KitchenMysteryMeat;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Effects;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Applies illegal sight transformation effects at the start of each day.
    /// </summary>
    public class ApplyIllegalSightEffects : StartOfDaySystem, IModSystem
    {
        /// <summary>
        /// Processes illegal-sight entities at the start of each day to handle their transitions.
        /// </summary>
        protected override void OnUpdate()
        {
            // Build query of illegal entities
            var query = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CIllegalSight>() }
            });

            using (NativeArray<Entity> allEntities = query.ToEntityArray(Allocator.Temp))
            {
                // Log the total number of illegal entities discovered for debugging purposes using the debug helper.
                DebugLogSystem.LogVerbose($"[ApplyIllegalSightEffects] Processing {allEntities.Length} illegal entities at day start.");

                // Guard: short-circuit when no illegal entities require processing.
                if (allEntities.Length > 0)
                {
                    // Create an EntityContext backed by the project's EntityManager
                    EntityContext ctx = new EntityContext(EntityManager);

                    for (int i = allEntities.Length - 1; i >= 0; --i)
                    {
                        Entity entity = allEntities[i];

                        // Emit a debug line for each entity as it is evaluated.
                        DebugLogSystem.LogVerbose($"[ApplyIllegalSightEffects] Evaluating entity {entity.Index}.");

                        // Handle illegal items by spawning rotten replacements.
                        if (ctx.Has<CItem>(entity))
                        {
                            DebugLogSystem.LogVerbose($"[ApplyIllegalSightEffects] Transforming illegal item entity {entity.Index}.");
                            CorpseEffects.TransformCorpse(ctx, entity);
                        }
                        // Handle illegal appliances that swap to their configured replacements.
                        else if (ctx.Has<CAppliance>(entity))
                        {
                            DebugLogSystem.LogVerbose($"[ApplyIllegalSightEffects] Replacing illegal appliance entity {entity.Index}.");
                            CorpseEffects.ReplaceWithAppliance(ctx, entity);
                        }
                        else
                        {
                            DebugLogSystem.LogVerbose($"[ApplyIllegalSightEffects] Entity {entity.Index} lacked item or appliance components.");
                        }
                    }
                }
            }
        }
    }
}
