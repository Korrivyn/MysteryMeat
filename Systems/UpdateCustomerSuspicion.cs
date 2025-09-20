using Kitchen;
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
                        // Remove from customer group
                        int targetedIndex = 0;
                        for (int j = groupMembers.Length - 1; j > -1; j--)
                        {
                            if (groupMembers[j].Customer != customer)
                                continue;
                            groupMembers.RemoveAt(j);
                            targetedIndex = j;
                            break;
                        }
                        // Remove from orders
                        if (RequireBuffer<CWaitingForItem>(cBelongsToGroup.Group, out DynamicBuffer<CWaitingForItem> waitingForItems))
                        {
                            for (int j = waitingForItems.Length - 1; j > -1; j--)
                            {
                                if (waitingForItems[j].MemberIndex != targetedIndex)
                                    continue;
                                waitingForItems.RemoveAt(j);
                                break;
                            }
                        }
                    }

                    // Make leave
                    susIndicator.IndicatorType = SuspicionIndicatorType.Alert;
                    EntityManager.SetComponentData(customer, susIndicator);

                    if (!Has<CCustomerLeaving>(customer))
                    {
                        EntityManager.AddComponent<CCustomerLeaving>(customer);
                    }

                    if (Has<CMoveToLocation>(customer))
                    {
                        EntityManager.RemoveComponent<CMoveToLocation>(customer);
                    }

                    if (!Has<CAlertedCustomer>(customer))
                    {
                        EntityManager.AddComponent<CAlertedCustomer>(customer);
                    }

                    if (Require<CBelongsToGroup>(customer, out CBelongsToGroup alertGroup))
                    {
                        EnsureGroupLeaves(alertGroup.Group);
                    }

                    CSoundEvent.Create(EntityManager, Mod.AlertSoundEvent);
                }
            }
        }

        private void EnsureGroupLeaves(Entity group)
        {
            if (group == default)
            {
                return;
            }

            if (!Has<CGroupStartLeaving>(group))
            {
                EntityManager.AddComponent<CGroupStartLeaving>(group);
            }

            if (!Has<CGroupLeaving>(group))
            {
                EntityManager.AddComponent<CGroupLeaving>(group);
            }

            if (!Has<CGroupStateChanged>(group))
            {
                EntityManager.AddComponent<CGroupStateChanged>(group);
            }

            if (Has<CGroupAwaitingOrder>(group))
            {
                EntityManager.RemoveComponent<CGroupAwaitingOrder>(group);
            }

            if (Has<CGroupReadyToOrder>(group))
            {
                EntityManager.RemoveComponent<CGroupReadyToOrder>(group);
            }

            if (Has<CQueuePosition>(group))
            {
                EntityManager.RemoveComponent<CQueuePosition>(group);
            }

            if (Require<CAssignedTable>(group, out CAssignedTable assignedTable) && assignedTable.Table != default)
            {
                EntityManager.RemoveComponent<CAssignedTable>(group);

                if (Has<COccupiedByGroup>(assignedTable.Table))
                {
                    EntityManager.RemoveComponent<COccupiedByGroup>(assignedTable.Table);
                }
            }

            if (Require<CAssignedMenu>(group, out CAssignedMenu assignedMenu) && assignedMenu.Menu != default)
            {
                EntityManager.RemoveComponent<CAssignedMenu>(group);

                if (Has<COccupiedByGroup>(assignedMenu.Menu))
                {
                    EntityManager.RemoveComponent<COccupiedByGroup>(assignedMenu.Menu);
                }
            }

            if (Require<CAssignedStand>(group, out CAssignedStand assignedStand) && assignedStand.Stand != default)
            {
                EntityManager.RemoveComponent<CAssignedStand>(group);

                if (Has<COccupiedByGroup>(assignedStand.Stand))
                {
                    EntityManager.RemoveComponent<COccupiedByGroup>(assignedStand.Stand);
                }
            }
        }
    }
}
