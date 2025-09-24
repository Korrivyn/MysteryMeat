using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Coordinates consumption of special sauce charges when extra customer orders are satisfied by tables.
    /// </summary>
    [UpdateAfter(typeof(GroupReceiveExtra))]
    public class SpecialSauceServing : GameSystemBase, IModSystem
    {
        private EntityQuery GroupQuery;
        public bool has_found_item;

        /// <summary>
        /// Builds the entity query that identifies customer groups waiting on items and assigned to tables.
        /// </summary>
        protected override void Initialise()
        {
            GroupQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CWaitingForItem), typeof(CGroupMember), typeof(CAssignedTable)));
        }

        /// <summary>
        /// Decrements sauce charges for each satisfied extra order while emitting diagnostics for missing holders or bottles.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> groups = GroupQuery.ToEntityArray(Allocator.TempJob);

            // Iterate through each dining group that is awaiting items to identify fulfilled extra orders.
            foreach (Entity group in groups)
            {
                DynamicBuffer<CWaitingForItem> orders = GetBuffer<CWaitingForItem>(group);
                DynamicBuffer<CGroupMember> groupMembers = GetBuffer<CGroupMember>(group);
                CAssignedTable cAssignedTable = GetComponent<CAssignedTable>(group);

                // Evaluate every outstanding order for the current group to detect extra satisfied dishes.
                for (int i = 0; i < orders.Length; i++)
                {
                    // Emit a verbose breadcrumb so debugging can trace which order is being analysed.
                    DebugLogSystem.LogVerbose($"[SpecialSauceServing] Evaluating order index {i} for group entity {group.Index}.");

                    // Guard: proceed only when the order has received the extra item that should consume sauce.
                    if (!orders[i].ExtraSatisfied)
                    {
                        continue;
                    }

                    // Ensure the assigned table exposes grab points that can hold sauce bottles.
                    if (!RequireBuffer<CTableSetGrabPoints>(cAssignedTable.Table, out var cTableSetGrabPoints))
                    {
                        DebugLogSystem.LogWarning($"[SpecialSauceServing] Table entity {cAssignedTable.Table.Index} has no grab points for group {group.Index}.");
                        continue;
                    }

                    // Inspect each grab point on the table to locate a special sauce holder.
                    foreach (var tableGrabPoint in cTableSetGrabPoints)
                    {
                        // Cache the grab point entity to streamline repeated references within the loop.
                        Entity grabPoint = tableGrabPoint;

                        // Guard: ensure a holder exists so sauce can be dispensed from the grab point.
                        if (!Require<CItemHolder>(grabPoint, out var citemHolder))
                        {
                            DebugLogSystem.LogWarning($"[SpecialSauceServing] Grab point entity {grabPoint.Index} lacks an item holder for table {cAssignedTable.Table.Index}.");
                            continue;
                        }

                        // Guard: confirm the holder currently stores a limited-use sauce bottle ready for dispensing.
                        if (citemHolder.HeldItem == Entity.Null)
                        {
                            DebugLogSystem.LogWarning($"[SpecialSauceServing] Item holder entity {grabPoint.Index} has no held item while serving group {group.Index}.");
                            continue;
                        }

                        // Guard: ensure the held item is a limited-use bottle so sauce charges can be consumed correctly.
                        if (!Require<CLimitedUseBottle>(citemHolder.HeldItem, out var limitedUseBottle))
                        {
                            DebugLogSystem.LogWarning($"[SpecialSauceServing] Held item entity {citemHolder.HeldItem.Index} on table {cAssignedTable.Table.Index} is not a limited-use bottle.");
                            continue;
                        }

                        Entity currentCustomer = groupMembers[orders[i].MemberIndex].Customer;

                        // Guard: skip decrementing when the same customer already consumed sauce for this order to avoid double charges.
                        if (limitedUseBottle.LastUsedByCustomer == currentCustomer)
                        {
                            DebugLogSystem.LogWarning($"[SpecialSauceServing] Duplicate sauce request detected for customer {currentCustomer.Index} on group {group.Index}.");
                            continue;
                        }

                        limitedUseBottle.FillAmount -= 1;
                        limitedUseBottle.LastUsedByCustomer = currentCustomer;

                        // Record the remaining sauce charge to support troubleshooting of consumption rates.
                        DebugLogSystem.LogVerbose($"[SpecialSauceServing] Consumed one sauce charge from bottle {citemHolder.HeldItem.Index}. Remaining: {limitedUseBottle.FillAmount}.");

                        EntityManager.SetComponentData(citemHolder.HeldItem, limitedUseBottle);
                    }
                }
            }
        }
    }
}
