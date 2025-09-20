// Systems/ApplyIllegalSightEffects.cs
// Invoker system: replaces the legacy StartOfDay/Overnight systems by invoking the new effect-style logic
// without using GameData or other legacy global lookups. This uses EntityContext (modern) which is used
// elsewhere in the project (see KillCustomers.cs).

using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Effects;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    public class ApplyIllegalSightEffects : StartOfDaySystem, IModSystem
    {
        protected override void OnUpdate()
        {
            // Build query of illegal entities
            var query = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CIllegalSight>() }
            });

            using (NativeArray<Entity> illegals = query.ToEntityArray(Allocator.Temp))
            {
                if (illegals.Length > 0)
                {
                    // Create an EntityContext backed by the project's EntityManager
                    EntityContext ctx = new EntityContext(EntityManager);

                    for (int i = illegals.Length - 1; i >= 0; --i)
                    {
                        Entity e = illegals[i];

                        if (ctx.Has<CItem>(e))
                        {
                            CorpseEffects.TransformCorpse(ctx, e);
                        }
                        else if (ctx.Has<CAppliance>(e))
                        {
                            CorpseEffects.ReplaceWithAppliance(ctx, e);
                        }
                    }
                }
            }
        }
    }
}
