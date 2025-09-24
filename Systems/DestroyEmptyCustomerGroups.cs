using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Cleans up customer group entities after all members have fled and no alerts remain in transit.
    /// </summary>
    [UpdateInGroup(typeof(DestructionGroup)), UpdateAfter(typeof(KillCustomers))]
    public class DestroyEmptyCustomerGroups : DaySystem, IModSystem
    {
        EntityQuery CustomerGroups;
        EntityQuery AlertedDiners;

        /// <summary>
        /// Builds queries used to locate customer groups and alerted diners for later evaluation.
        /// </summary>
        protected override void Initialise()
        {
            base.Initialise();

            CustomerGroups = GetEntityQuery(typeof(CCustomerGroup));
            AlertedDiners = GetEntityQuery(new QueryHelper()
                .All(typeof(CAlertedCustomer), typeof(CBelongsToGroup)));
        }

        /// <summary>
        /// Destroys empty groups whose members have either fled or been fully cleaned up.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _customerGroups = CustomerGroups.ToEntityArray(Allocator.Temp);
            using NativeArray<CBelongsToGroup> _alertedDiners = AlertedDiners.ToComponentDataArray<CBelongsToGroup>(Allocator.Temp);

            for (int i = _customerGroups.Length - 1; i > -1; i--)
            {
                Entity customerGroup = _customerGroups[i];

                // Guard: ensure the group exposes its membership buffer before attempting cleanup.
                if (RequireBuffer<CGroupMember>(customerGroup, out DynamicBuffer<CGroupMember> groupMembers))
                {
                    // Guard: only destroy the group when no members remain.
                    if (groupMembers.Length <= 0)
                    {
                        bool hasAlertedMembersInFlight = false;

                        for (int j = 0; j < _alertedDiners.Length; j++)
                        {
                            if (_alertedDiners[j].Group == customerGroup)
                            {
                                hasAlertedMembersInFlight = true;
                                break;
                            }
                        }

                        // Guard: skip destruction when alerted customers are still leaving the restaurant.
                        if (hasAlertedMembersInFlight)
                        {
                            DebugLogSystem.LogVerbose($"Deferred destruction for group {customerGroup.Index} because alerted diners remain in flight.");
                            continue;
                        }

                        // Guard: destroy any indicator entity before removing the group itself.
                        if (Require<CHasIndicator>(customerGroup, out CHasIndicator cHasIndicator))
                        {
                            EntityManager.DestroyEntity(cHasIndicator.Indicator);
                            DebugLogSystem.LogVerbose($"Removed indicator {cHasIndicator.Indicator.Index} prior to destroying group {customerGroup.Index}.");
                        }

                        EntityManager.DestroyEntity(customerGroup);
                        DebugLogSystem.LogVerbose($"Destroyed empty group {customerGroup.Index}.");
                    }
                }
                else
                {
                    DebugLogSystem.LogWarning($"Could not access CGroupMember buffer for group {customerGroup.Index}; destruction skipped.");
                }
            }
        }
    }
}
