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
    public partial class HandlePersistentCorpses : SystemBase, IModSystem
    {
        private EndSimulationEntityCommandBufferSystem _endEcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _endEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (!HasStatus((RestaurantStatus)VariousUtils.GetID("persistentcorpses")))
                return;

            var ecb = _endEcbSystem.CreateCommandBuffer().AsParallelWriter();

            var gameData = GameData.Main;

            Entities
                .WithName("HandlePersistentCorpses")
                .WithAll<CIllegalSight>()
                .ForEach((Entity entity, int entityInQueryIndex, in CIllegalSight illegalSight) =>
                {
                    // Skip if TurnIntoOnDayStart doesn’t resolve to an appliance or item
                    if (!gameData.TryGet(illegalSight.TurnIntoOnDayStart, out Appliance _, false) &&
                        !gameData.TryGet(illegalSight.TurnIntoOnDayStart, out Item _, false))
                        return;

                    // ----- ITEM CASE -----
                    if (HasComponent<CItem>(entity))
                    {
                        if (HasComponent<CHeldBy>(entity) &&
                            RequireBuffer<CHeldBy>(entity, out var holder) &&
                            !HasComponent<CPreservesContentsOvernight>(holder.Entity))
                        {
                            ecb.AddComponent(entityInQueryIndex, entity, new CChangeItemType
                            {
                                NewID = illegalSight.TurnIntoOnDayStart,
                            });

                            if (HasComponent<CSplittableItem>(entity))
                            {
                                var split = GetComponent<CSplittableItem>(entity);
                                ecb.AddComponent(entityInQueryIndex, entity, new CPersistPortions
                                {
                                    RemainingCount = split.RemainingCount,
                                    TotalCount = split.TotalCount,
                                });
                            }
                        }
                    }
                    // ----- APPLIANCE CASE -----
                    else if (HasComponent<CAppliance>(entity))
                    {
                        if (HasComponent<CPosition>(entity))
                        {
                            var pos = GetComponent<CPosition>(entity);

                            var newEntity = ecb.CreateEntity(entityInQueryIndex);
                            ecb.AddComponent(entityInQueryIndex, newEntity, new CCreateAppliance
                            {
                                ID = illegalSight.TurnIntoOnDayStart,
                                ForceLayer = OccupancyLayer.Ceiling
                            });
                            ecb.AddComponent(entityInQueryIndex, newEntity, pos);

                            ecb.DestroyEntity(entityInQueryIndex, entity);
                        }
                    }
                })
                .ScheduleParallel();

            _endEcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
