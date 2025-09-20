using Kitchen;
using KitchenData;
using KitchenLib.References;
using KitchenLib.Utils;
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

namespace KitchenMysteryMeat.Systems
{
	[UpdateBefore(typeof(DestroyAfterDuration))]
    public class AddMessToBottle : GameSystemBase, IModSystem
    {
        private EntityQuery ApplianceQuery;
        protected override void Initialise()
        {
            ApplianceQuery = GetEntityQuery(new QueryHelper()
                            .All(typeof(CFillsBottle), typeof(CAppliance), typeof(CTakesDuration), typeof(CBeingActedOnBy)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> _appliances = ApplianceQuery.ToEntityArray(Allocator.TempJob);

            foreach (Entity appliance in _appliances)
            {
                CAppliance cAppliance = GetComponent<CAppliance>(appliance);
                CFillsBottle cFillsBottle = GetComponent<CFillsBottle>(appliance);
                DynamicBuffer<CBeingActedOnBy> actors = GetBuffer<CBeingActedOnBy>(appliance);
                CTakesDuration duration = GetComponent<CTakesDuration>(appliance);

                if (!duration.Active || duration.Remaining > 0f)
                {
                    continue;
                }

                if (actors.IsEmpty)
                {
                    continue;
                }

                for (int i = 0; i < actors.Length; i++)
                {
                    if (Require<CItemHolder>(actors[i].Interactor, out var itemHolder))
                    {
                        /*if (GetComponent<CItem>(itemHolder.HeldItem).ID != cFillsBottle.BottleID)
                            continue;*/

                        if (Require<CEmptyBottle>(itemHolder.HeldItem, out var emptyBottle))
                        {
                            EntityManager.AddComponentData<CChangeItemType>(itemHolder.HeldItem, new CChangeItemType()
                            {
                                NewID = emptyBottle.FullBottleID,
                            });
                            EntityManager.RemoveComponent<CEmptyBottle>(itemHolder.HeldItem);
                            continue;
                        }

                        if (Require<CLimitedUseBottle>(itemHolder.HeldItem, out var bottle)) 
                        {
                            bottle.FillAmount = bottle.Limit;

                            if (actors[i].Interactor != Entity.Null)
                                EntityManager.SetComponentData(itemHolder.HeldItem, bottle);
                        }
                    }
                }

                EntityManager.RemoveComponent<CFillsBottle>(appliance);
            }
        }
    }
}
