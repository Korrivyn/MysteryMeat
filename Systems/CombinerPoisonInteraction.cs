using Kitchen;
using KitchenData;
using KitchenMods;
using KitchenMysteryMeat.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenMysteryMeat.Systems
{
    [UpdateAfter(typeof(AttemptInteraction))]
    [UpdateInGroup(typeof(InteractionGroup), OrderFirst = true)]
    public class CombinerPoisonInteraction : GenericSystemBase, IModSystem
    {
        private EntityQuery InteractivesQuery;

        protected override void Initialise()
        {
            base.Initialise();
            this.InteractivesQuery = GetEntityQuery(new QueryHelper()
                            .All(typeof(CAutomatedInteractor), typeof(CPosition), typeof(CItemHolder)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> _automatedInteractors = InteractivesQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity automatedInteractor in _automatedInteractors)
            {
                CPosition position = GetComponent<CPosition>(automatedInteractor);
                CAutomatedInteractor auto = GetComponent<CAutomatedInteractor>(automatedInteractor);
                CItemHolder itemHolder = GetComponent<CItemHolder>(automatedInteractor);

                if (!Has<CPoisonBottle>(itemHolder.HeldItem))
                {
                    continue;
                }

                CAutomatedInteractorRandomActiveInterval cautomatedInteractorRandomActiveInterval;
                if (base.Require<CAutomatedInteractorRandomActiveInterval>(automatedInteractor, out cautomatedInteractorRandomActiveInterval) && !cautomatedInteractorRandomActiveInterval.Active)
                {
                    return;
                }
                Vector3 forwardPosition = position.ForwardPosition;
                Entity occupant = TileManager.GetOccupant(forwardPosition, OccupancyLayer.Default);
                if (!TileManager.CanReach(position, forwardPosition, false))
                {
                    return;
                }
                if (occupant == default(Entity))
                {
                    return;
                }
                EntityManager.AddComponentData<CAttemptingInteraction>(automatedInteractor, new CAttemptingInteraction
                {
                    Target = occupant,
                    Type = auto.Type,
                    IsHeld = auto.IsHeld,
                    Location = forwardPosition,
                    Mode = InteractionMode.Items,
                    TransferOnly = auto.TransferOnly
                });
                if (auto.Type == InteractionType.Grab)
                {
                    if (base.Require<CItemHolder>(occupant, out var occupantItem) && occupantItem.HeldItem != Entity.Null && !base.Has<CPoisoned>(occupantItem.HeldItem))
                    {
                        EntityManager.AddComponent<CPoisoned>(occupantItem.HeldItem);
                        CSoundEvent.Create(EntityManager, Mod.PoisonSoundEvent);
                    }
                }
            }
        }
    }
}
