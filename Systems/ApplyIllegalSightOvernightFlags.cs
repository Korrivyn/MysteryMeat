using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    public class ApplyIllegalSightOvernightFlags : GameSystemBase, IModSystem
    {
        private EntityQuery IllegalEntities;

        protected override void Initialise()
        {
            base.Initialise();

            IllegalEntities = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CIllegalSight>() }
            });
        }

        protected override void OnUpdate()
        {
            bool persistent = HasStatus((RestaurantStatus)VariousUtils.GetID("persistentcorpses"));

            using NativeArray<Entity> illegals = IllegalEntities.ToEntityArray(Allocator.Temp);
            if (illegals.Length == 0)
                return;

            for (int i = 0; i < illegals.Length; ++i)
            {
                Entity entity = illegals[i];

                if (EntityManager.HasComponent<CItem>(entity))
                {
                    if (persistent)
                    {
                        if (!EntityManager.HasComponent<CPreservedOvernight>(entity))
                        {
                            EntityManager.AddComponentData(entity, new CPreservedOvernight());
                        }
                    }
                    else if (EntityManager.HasComponent<CPreservedOvernight>(entity))
                    {
                        EntityManager.RemoveComponent<CPreservedOvernight>(entity);
                    }
                }

                if (EntityManager.HasComponent<CAppliance>(entity))
                {
                    if (persistent)
                    {
                        if (EntityManager.HasComponent<CDestroyApplianceAtNight>(entity))
                        {
                            EntityManager.RemoveComponent<CDestroyApplianceAtNight>(entity);
                        }
                    }
                    else if (!EntityManager.HasComponent<CDestroyApplianceAtNight>(entity))
                    {
                        EntityManager.AddComponentData(entity, new CDestroyApplianceAtNight());
                    }
                }
            }
        }
    }
}
