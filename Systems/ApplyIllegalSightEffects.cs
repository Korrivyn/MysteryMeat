// Systems/ApplyIllegalSightEffects.cs
// Invoker system: replaces the legacy StartOfDay/Overnight systems by invoking the new effect-style logic
// without using GameData or other legacy global lookups. This uses EntityContext (modern) which is used
// elsewhere in the project (see KillCustomers.cs).

using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Effects;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    public class ApplyIllegalSightEffects : NightSystem, IModSystem
    {
        private EntityQuery ItemHolderEntities;

        protected override void Initialise()
        {
            base.Initialise();

            ItemHolderEntities = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CItemHolder>() }
            });
        }

        protected override void OnUpdate()
        {
            // Build query of illegal entities
            var query = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CIllegalSight>() }
            });

            Dictionary<Entity, Entity> heldItemLookup = BuildHeldItemLookup();
            List<Entity> holderBuffer = new List<Entity>(capacity: 2);

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
                            List<Entity> holders = CorpseStorageUtils.CollectHolderEntities(EntityManager, e, holderBuffer, heldItemLookup);
                            bool hasTruePreserver;
                            bool hasTemporaryPreserver;

                            if (CorpseStorageUtils.AnalyseHolderPreservation(EntityManager, holders, out hasTruePreserver, out hasTemporaryPreserver))
                            {
                                if (hasTemporaryPreserver)
                                {
                                    CorpseStorageUtils.RemoveTemporaryHolderPreservation(EntityManager, holders);
                                }

                                if (hasTruePreserver)
                                {
                                    continue;
                                }
                            }

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

        private Dictionary<Entity, Entity> BuildHeldItemLookup()
        {
            Dictionary<Entity, Entity> lookup = new Dictionary<Entity, Entity>();

            if (ItemHolderEntities == null || ItemHolderEntities.IsEmptyIgnoreFilter)
            {
                return lookup;
            }

            using NativeArray<Entity> holderEntities = ItemHolderEntities.ToEntityArray(Allocator.Temp);
            using NativeArray<CItemHolder> holderData = ItemHolderEntities.ToComponentDataArray<CItemHolder>(Allocator.Temp);

            int length = holderEntities.Length < holderData.Length ? holderEntities.Length : holderData.Length;

            for (int i = 0; i < length; i++)
            {
                Entity heldItem = holderData[i].HeldItem;
                if (heldItem == Entity.Null)
                {
                    continue;
                }

                lookup[heldItem] = holderEntities[i];
            }

            return lookup;
        }

    }
}
