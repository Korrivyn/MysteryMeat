using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class HandleOvernightCorpses : SystemBase, IModSystem
    {
        private EndSimulationEntityCommandBufferSystem _endEcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _endEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _endEcbSystem.CreateCommandBuffer().AsParallelWriter();

            bool persistent = HasStatus((RestaurantStatus)VariousUtils.GetID("persistentcorpses"));

            Entities
                .WithName("HandleOvernightCorpses")
                .WithAll<CIllegalSight>()
                .ForEach((Entity entity, int entityInQueryIndex) =>
                {
                    if (HasComponent<CItem>(entity))
                    {
                        if (persistent)
                        {
                            if (!HasComponent<CPreservedOvernight>(entity))
                                ecb.AddComponent<CPreservedOvernight>(entityInQueryIndex, entity);
                        }
                        else
                        {
                            if (HasComponent<CPreservedOvernight>(entity))
                                ecb.RemoveComponent<CPreservedOvernight>(entityInQueryIndex, entity);
                        }
                    }

                    if (HasComponent<CAppliance>(entity))
                    {
                        if (persistent)
                        {
                            if (HasComponent<CDestroyApplianceAtNight>(entity))
                                ecb.RemoveComponent<CDestroyApplianceAtNight>(entityInQueryIndex, entity);
                        }
                        else
                        {
                            if (!HasComponent<CDestroyApplianceAtNight>(entity))
                                ecb.AddComponent<CDestroyApplianceAtNight>(entityInQueryIndex, entity);
                        }
                    }
                })
                .ScheduleParallel();

            _endEcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
