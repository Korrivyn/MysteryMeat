﻿using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Enums;
using KitchenMysteryMeat.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Sony.NP.Matching;

namespace KitchenMysteryMeat.Systems
{
    public class UpdateCustomerSuspicion : DaySystem, IModSystem
    {
        EntityQuery SuspicionIndicators;
        protected override void Initialise()
        {
            base.Initialise();
            SuspicionIndicators = GetEntityQuery(new QueryHelper()
                            .All(typeof(CCustomer), typeof(CSuspicionIndicator)));

        }
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _suspicionIndicators = SuspicionIndicators.ToEntityArray(Allocator.Temp);

            foreach (Entity customer in _suspicionIndicators)
            {
                CSuspicionIndicator susIndicator = EntityManager.GetComponentData<CSuspicionIndicator>(customer);

                if (susIndicator.TotalTime <= 0.0f || susIndicator.IndicatorType == SuspicionIndicatorType.Alert)
                    continue;

                
                if (susIndicator.SeenIllegalThing != null && EntityManager.Exists((Entity)susIndicator.SeenIllegalThing) && !Has<CStoredBy>((Entity)susIndicator.SeenIllegalThing)) 
                {
                    susIndicator.RemainingTime = Mathf.Clamp(susIndicator.RemainingTime - Time.DeltaTime, 0.0f, susIndicator.TotalTime);
                }
                else 
                {
                    // Divide delta time by 2 to make suspicion go down slower
                    susIndicator.RemainingTime = Mathf.Clamp(susIndicator.RemainingTime + (Time.DeltaTime / 2.0f), 0.0f, susIndicator.TotalTime);
                }

                EntityManager.SetComponentData(customer, susIndicator);

                if (susIndicator.RemainingTime <= 0.0f)
                {
                    // Run away and make into alert indicator

                    // Remove customer from group
                    if (Require<CBelongsToGroup>(customer, out CBelongsToGroup cBelongsToGroup) && RequireBuffer<CGroupMember>(cBelongsToGroup.Group, out DynamicBuffer<CGroupMember> groupMembers))
                    {
                        for (int j = groupMembers.Length - 1; j > -1; j--)
                        {
                            if (groupMembers[j].Customer != customer)
                                continue;
                            groupMembers.RemoveAt(j);
                            break;
                        }
                    }

                    // Make leave
                    susIndicator.IndicatorType = SuspicionIndicatorType.Alert;
                    EntityManager.AddComponent<CCustomerLeaving>(customer);
                    EntityManager.AddComponent<CRunningAway>(customer);
                    EntityManager.SetComponentData(customer, susIndicator);

                    CSoundEvent.Create(EntityManager, Mod.AlertSoundEvent);
                }
            }
        }
    }
}
