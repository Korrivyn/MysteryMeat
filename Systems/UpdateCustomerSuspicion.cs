using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Enums;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Drives customer suspicion countdowns, escalates diners to alerts when timers expire,
    /// and coordinates the departure workflow for their associated groups.
    /// </summary>
    public class UpdateCustomerSuspicion : DaySystem, IModSystem
    {
        private EntityQuery SuspicionIndicators;

        /// <summary>
        /// Builds the query used to evaluate all active suspicion indicators in the restaurant.
        /// </summary>
        protected override void Initialise()
        {
            base.Initialise();
            SuspicionIndicators = GetEntityQuery(new QueryHelper()
                .All(typeof(CCustomer), typeof(CSuspicionIndicator)));
        }

        /// <summary>
        /// Reduces or recovers suspicion timers, converts expired diners into alerts, and
        /// ensures their groups initiate a coordinated exit.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> suspicionIndicators = SuspicionIndicators.ToEntityArray(Allocator.Temp);

            // Iterate through each diner indicator so the system can manage suspicion decay and escalation.
            foreach (Entity customer in suspicionIndicators)
            {
                CSuspicionIndicator susIndicator = EntityManager.GetComponentData<CSuspicionIndicator>(customer);

                // Skip indicators that have already resolved or escalated to alerts.
                if (susIndicator.TotalTime <= 0.0f || susIndicator.IndicatorType == SuspicionIndicatorType.Alert)
                {
                    continue;
                }

                // Adjust the suspicion timer according to whether the illegal activity remains visible.
                if (susIndicator.SeenIllegalThing != null && EntityManager.Exists((Entity)susIndicator.SeenIllegalThing) && !Has<CStoredBy>((Entity)susIndicator.SeenIllegalThing))
                {
                    susIndicator.RemainingTime = Mathf.Clamp(susIndicator.RemainingTime - Time.DeltaTime, 0.0f, susIndicator.TotalTime);
                    DebugLogSystem.LogVerbose($"[UpdateCustomerSuspicion] Customer {customer.Index} suspicion ticking down to {susIndicator.RemainingTime:F2}s.");
                }
                else
                {
                    // Divide delta time by 2 to make suspicion go down slower
                    susIndicator.RemainingTime = Mathf.Clamp(susIndicator.RemainingTime + (Time.DeltaTime / 2.0f), 0.0f, susIndicator.TotalTime);
                    DebugLogSystem.LogVerbose($"[UpdateCustomerSuspicion] Customer {customer.Index} suspicion recovering to {susIndicator.RemainingTime:F2}s.");
                }

                EntityManager.SetComponentData(customer, susIndicator);

                // Escalate to an alert state when the suspicion timer has expired.
                if (susIndicator.RemainingTime <= 0.0f)
                {
                    // Remove customer orders but leave them in the group so they continue to count as present.
                    if (Require<CBelongsToGroup>(customer, out CBelongsToGroup cBelongsToGroup))
                    {
                        // Guard against missing group buffers while preparing the evacuation.
                        if (!RequireBuffer<CGroupMember>(cBelongsToGroup.Group, out DynamicBuffer<CGroupMember> groupMembers))
                        {
                            DebugLogSystem.LogError($"[UpdateCustomerSuspicion] Group {cBelongsToGroup.Group.Index} missing CGroupMember buffer while evacuating customer {customer.Index}.");
                        }
                        else
                        {
                            // Find the alerted customer's index within the group without removing them from the buffer.
                            int targetedIndex = -1;
                            for (int index = groupMembers.Length - 1; index > -1; index--)
                            {
                                // Identify the fleeing customer entry within the group membership buffer.
                                if (groupMembers[index].Customer != customer)
                                {
                                    continue;
                                }

                                targetedIndex = index;
                                break;
                            }

                            // Remove any outstanding orders linked to the fleeing customer so staff stop trying to serve them.
                            if (targetedIndex >= 0 && RequireBuffer<CWaitingForItem>(cBelongsToGroup.Group, out DynamicBuffer<CWaitingForItem> waitingForItems))
                            {
                                // Clear orders associated with the leaving diner to prevent further service attempts.
                                for (int index = waitingForItems.Length - 1; index > -1; index--)
                                {
                                    // Match and remove any outstanding order referencing the targeted diner.
                                    if (waitingForItems[index].MemberIndex != targetedIndex)
                                    {
                                        continue;
                                    }

                                    waitingForItems.RemoveAt(index);
                                }
                            }
                            // Warn when the waiting buffer is missing yet the diner was located in the group membership list.
                            else if (targetedIndex >= 0)
                            {
                                DebugLogSystem.LogWarning($"[UpdateCustomerSuspicion] Group {cBelongsToGroup.Group.Index} missing CWaitingForItem buffer while clearing orders for customer {customer.Index}.");
                            }
                        }
                    }
                    else
                    {
                        DebugLogSystem.LogWarning($"[UpdateCustomerSuspicion] Customer {customer.Index} lacked CBelongsToGroup during alert escalation.");
                    }

                    DebugLogSystem.LogVerbose($"[UpdateCustomerSuspicion] Customer {customer.Index} converting to alert state.");

                    // Make leave by marking the indicator as alerted.
                    susIndicator.IndicatorType = SuspicionIndicatorType.Alert;
                    EntityManager.SetComponentData(customer, susIndicator);

                    // Add leave tag when the diner has not already started fleeing.
                    if (!Has<CCustomerLeaving>(customer))
                    {
                        EntityManager.AddComponent<CCustomerLeaving>(customer);
                    }

                    // Remove any current movement targets so the leave behaviour can take control.
                    if (Has<CMoveToLocation>(customer))
                    {
                        EntityManager.RemoveComponent<CMoveToLocation>(customer);
                    }

                    // Tag the diner so other systems treat them as part of the forced-leave sequence.
                    if (!Has<CAlertedCustomer>(customer))
                    {
                        EntityManager.AddComponent<CAlertedCustomer>(customer);
                    }

                    DebugLogSystem.LogInfo($"[UpdateCustomerSuspicion] Customer {customer.Index} is fleeing after reaching maximum suspicion.");
                    // Notify the group to leave when a member has fled.
                    if (Require<CBelongsToGroup>(customer, out CBelongsToGroup alertGroup))
                    {
                        EnsureGroupLeaves(alertGroup.Group);
                    }

                    CSoundEvent.Create(EntityManager, Mod.AlertSoundEvent);
                }
            }
        }

        /// <summary>
        /// Forces a group to transition into a leaving state and clears any lingering table assignments.
        /// </summary>
        private void EnsureGroupLeaves(Entity group)
        {
            // Abort when the supplied entity is invalid because no group can be updated.
            if (group == default)
            {
                return;
            }

            // Mark the group as starting the leave process when not already initiated.
            if (!Has<CGroupStartLeaving>(group))
            {
                EntityManager.AddComponent<CGroupStartLeaving>(group);
            }

            // Guarantee the group holds the leaving tag so other systems process their exit.
            if (!Has<CGroupLeaving>(group))
            {
                EntityManager.AddComponent<CGroupLeaving>(group);
            }

            // Flag that the group state changed to trigger dependent systems.
            if (!Has<CGroupStateChanged>(group))
            {
                EntityManager.AddComponent<CGroupStateChanged>(group);
            }

            // Remove ordering states to avoid conflicting with the evacuation flow.
            if (Has<CGroupAwaitingOrder>(group))
            {
                EntityManager.RemoveComponent<CGroupAwaitingOrder>(group);
            }

            // Clear the ready-to-order state because the group is no longer being served.
            if (Has<CGroupReadyToOrder>(group))
            {
                EntityManager.RemoveComponent<CGroupReadyToOrder>(group);
            }

            // Free their queue position so new parties can advance.
            if (Has<CQueuePosition>(group))
            {
                EntityManager.RemoveComponent<CQueuePosition>(group);
            }

            // Release any assigned table and free the associated occupancy marker.
            if (Require<CAssignedTable>(group, out CAssignedTable assignedTable) && assignedTable.Table != default)
            {
                EntityManager.RemoveComponent<CAssignedTable>(group);

                // Clear the table occupancy so other groups can be seated.
                if (Has<COccupiedByGroup>(assignedTable.Table))
                {
                    EntityManager.RemoveComponent<COccupiedByGroup>(assignedTable.Table);
                }
            }

            // Release any assigned menu and free its occupancy marker.
            if (Require<CAssignedMenu>(group, out CAssignedMenu assignedMenu) && assignedMenu.Menu != default)
            {
                EntityManager.RemoveComponent<CAssignedMenu>(group);

                // Clear the menu occupancy so future groups can access it.
                if (Has<COccupiedByGroup>(assignedMenu.Menu))
                {
                    EntityManager.RemoveComponent<COccupiedByGroup>(assignedMenu.Menu);
                }
            }

            // Release any assigned stand and free its occupancy marker.
            if (Require<CAssignedStand>(group, out CAssignedStand assignedStand) && assignedStand.Stand != default)
            {
                EntityManager.RemoveComponent<CAssignedStand>(group);

                // Clear the stand occupancy so it becomes available to new groups.
                if (Has<COccupiedByGroup>(assignedStand.Stand))
                {
                    EntityManager.RemoveComponent<COccupiedByGroup>(assignedStand.Stand);
                }
            }
        }
    }
}
