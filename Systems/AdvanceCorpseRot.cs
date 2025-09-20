using Kitchen;
using KitchenData;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Effects;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    public class AdvanceCorpseRot : DaySystem, IModSystem
    {
        private EntityQuery PendingRots;

        protected override void Initialise()
        {
            base.Initialise();

            PendingRots = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<CPendingCorpseRot>()
                }
            });
        }

        protected override void OnUpdate()
        {
            if (PendingRots.IsEmpty)
            {
                return;
            }

            float deltaTime = Time.DeltaTime;
            EntityContext ctx = new EntityContext(EntityManager);

            using NativeArray<Entity> entities = PendingRots.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!EntityManager.Exists(entity))
                {
                    continue;
                }

                CPendingCorpseRot pending = EntityManager.GetComponentData<CPendingCorpseRot>(entity);

                if (pending.Duration <= 0f)
                {
                    pending.Duration = CorpseEffects.DefaultRotFadeDuration;
                }

                pending.Elapsed += deltaTime;
                bool completed = pending.Elapsed >= pending.Duration;
                EntityManager.SetComponentData(entity, pending);

                if (!completed)
                {
                    continue;
                }

                EntityManager.RemoveComponent<CPendingCorpseRot>(entity);

                if (!EntityManager.Exists(entity))
                {
                    continue;
                }

                if (pending.TargetItemID > 0 && ctx.Has<CItem>(entity))
                {
                    ctx.Set(entity, new CChangeItemType
                    {
                        NewID = pending.TargetItemID
                    });

                    if (pending.PreservePortions && ctx.Has<CSplittableItem>(entity))
                    {
                        CSplittableItem split = ctx.Get<CSplittableItem>(entity);
                        ctx.Set(entity, new CPersistPortions
                        {
                            RemainingCount = split.RemainingCount,
                            TotalCount = split.TotalCount
                        });
                    }
                }
                else if (pending.TargetApplianceID > 0 && ctx.Has<CAppliance>(entity) && ctx.Has<CPosition>(entity))
                {
                    CPosition pos = ctx.Get<CPosition>(entity);
                    Entity newEntity = ctx.CreateEntity();
                    ctx.Set(newEntity, new CCreateAppliance
                    {
                        ID = pending.TargetApplianceID,
                        ForceLayer = OccupancyLayer.Ceiling
                    });
                    ctx.Set(newEntity, pos);
                    ctx.Destroy(entity);
                }
            }
        }
    }
}
