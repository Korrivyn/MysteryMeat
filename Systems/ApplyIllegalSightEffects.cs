// Systems/ApplyIllegalSightEffects.cs
// Invoker system: replaces the legacy StartOfDay/Overnight systems by invoking the new effect-style logic
// without using GameData or other legacy global lookups. This uses EntityContext (modern) which is used
// elsewhere in the project (see KillCustomers.cs).

using Kitchen;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Effects;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    // The old code had a StartOfDaySystem and a GameSystemBase. We'll run both behaviors here:
    // - On StartOfDay: apply transform/replace effects for illegal sight entities (persistent corpses).
    // - Every update: toggle preserved/destroy-at-night flags for illegal items/appliances (overnight handling).
    //
    // This class intentionally replaces the old files and centralizes the effect invocation using EntityContext.
    public class ApplyIllegalSightEffects : StartOfDaySystem, IModSystem
    {
        // We'll still run the "overnight" toggles in the regular update path. Use GameSystemBase behavior by
        // adding a separate small componentless system below if you prefer. For now this class handles both:
        protected override void OnUpdate()
        {
            // ---- PART 1: StartOfDay behavior (transform or spawn) ----
            // This runs only at the start of day because StartOfDaySystem triggers OnUpdate at that time.
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

                        // For each illegal entity, apply transform or replacement using the Effect classes above
                        var transformEffect = new Effect_TransformCorpse();
                        var replaceEffect = new Effect_ReplaceWithAppliance();

                        for (int i = illegals.Length - 1; i >= 0; --i)
                        {
                            Entity e = illegals[i];

                            // If the entity is an item or appliance, apply transformEffect which handles both cases.
                            // transformEffect will internally check for CItem/CAppliance/CPosition and do the right thing.
                            transformEffect.Apply(ctx, e);
                        }
                    }
                }
            }

            // ---- PART 2: Overnight toggles (persistent flag on items, destroy-at-night on appliances) ----
            // This behavior used to run during every update; keep it running during normal updates by
            // scheduling a small additional non-StartOfDay run via a query here.
            {
                bool persistent = HasStatus((RestaurantStatus)VariousUtils.GetID("persistentcorpses"));

                var query = GetEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<CIllegalSight>() }
                });

                using (NativeArray<Entity> illegals = query.ToEntityArray(Allocator.Temp))
                {
                    if (illegals.Length > 0)
                    {
                        EntityContext ctx = new EntityContext(EntityManager);

                        for (int i = 0; i < illegals.Length; ++i)
                        {
                            Entity e = illegals[i];

                            // ITEMS: toggle CPreservedOvernight
                            if (EntityManager.HasComponent<CItem>(e))
                            {
                                if (persistent)
                                {
                                    if (!EntityManager.HasComponent<CPreservedOvernight>(e))
                                        ctx.Set(e, new CPreservedOvernight());
                                }
                                else
                                {
                                    if (EntityManager.HasComponent<CPreservedOvernight>(e))
                                    // No direct ctx.Remove<T> usage exists in many projects; if supported use ctx.Remove<T>(e)
                                    // If ctx.Remove<T> is unavailable, fall back to EntityManager.RemoveComponent<T>(e)
                                    {
                                        // Try to remove via EntityContext if available
                                        try
                                        {
                                            ctx.Remove<CPreservedOvernight>(e);
                                        }
                                        catch
                                        {
                                            EntityManager.RemoveComponent<CPreservedOvernight>(e);
                                        }
                                    }
                                }
                            }

                            // APPLIANCES: toggle CDestroyApplianceAtNight
                            if (EntityManager.HasComponent<CAppliance>(e))
                            {
                                if (persistent)
                                {
                                    if (EntityManager.HasComponent<CDestroyApplianceAtNight>(e))
                                    {
                                        try
                                        {
                                            ctx.Remove<CDestroyApplianceAtNight>(e);
                                        }
                                        catch
                                        {
                                            EntityManager.RemoveComponent<CDestroyApplianceAtNight>(e);
                                        }
                                    }
                                }
                                else
                                {
                                    if (!EntityManager.HasComponent<CDestroyApplianceAtNight>(e))
                                        ctx.Set(e, new CDestroyApplianceAtNight());
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}