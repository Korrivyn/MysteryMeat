using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Appliances;
using KitchenMysteryMeat.Systems.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Cleans up murdered customers by spawning corpses, removing them from groups, and tidying
    /// lingering orders so the dining experience remains coherent after violent incidents.
    /// </summary>
    [UpdateInGroup(typeof(DestructionGroup), OrderFirst = true)]
    public class KillCustomers : DaySystem, IModSystem
    {
        EntityQuery CustomersToKill;
        EntityQuery OrderIndicators;

        /// <summary>
        /// Prepares entity queries that identify slain customers and their associated order
        /// indicators so later updates can react efficiently.
        /// </summary>
        protected override void Initialise()
        {
            base.Initialise();
            CustomersToKill = GetEntityQuery(typeof(CCustomer), typeof(CKilled));
            OrderIndicators = GetEntityQuery(typeof(CHasItemCollectionIndicator));
        }

        /// <summary>
        /// Processes murdered customers by creating corpses, removing them from their dining
        /// groups, and clearing related orders while emitting detailed diagnostics.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _customers = CustomersToKill.ToEntityArray(Allocator.Temp);
            using NativeArray<Entity> _orderIndicators = OrderIndicators.ToEntityArray(Allocator.Temp);
            EntityContext ctx = new EntityContext(EntityManager);

            // Announce the number of slain customers being processed to aid situational awareness.
            DebugLogSystem.LogInfo($"[KillCustomers] Processing {_customers.Length} murdered customers.");

            // Iterate through each dead customer to orchestrate cleanup and corpse creation.
            for (int i = 0; i < _customers.Length; i++)
            {
                Entity customer = _customers[i];

                CPosition customerPosition = EntityManager.GetComponentData<CPosition>(customer);
                CKilled cKilled = EntityManager.GetComponentData<CKilled>(customer);

                // Provide breadcrumbs for each customer so verbose logs expose bloody state.
                DebugLogSystem.LogVerbose($"[KillCustomers] Handling customer {customer.Index} (bloody: {cKilled.Bloody}).");

                CreateCorpse(ctx, customerPosition, cKilled.Bloody);

                // Ensure the customer still references a dining group before attempting removal.
                if (!Require(customer, out CBelongsToGroup belongsToGroup))
                {
                    DebugLogSystem.LogWarning($"[KillCustomers] Customer {customer.Index} missing CBelongsToGroup; skipping group cleanup.");
                    continue;
                }

                // Confirm the dining group exposes its member buffer so we can excise the victim.
                if (!RequireBuffer(belongsToGroup.Group, out DynamicBuffer<CGroupMember> groupMembers))
                {
                    DebugLogSystem.LogWarning($"[KillCustomers] Group {belongsToGroup.Group.Index} missing CGroupMember buffer for customer {customer.Index}.");
                    continue;
                }

                int targetedIndex = 0;

                // Walk the group members in reverse to locate and remove the murdered customer safely.
                for (int j = groupMembers.Length - 1; j > -1; j--)
                {
                    // Skip over surviving members until the slain customer is encountered.
                    if (groupMembers[j].Customer != customer)
                        continue;
                    groupMembers.RemoveAt(j);
                    targetedIndex = j;
                    break;
                }

                // Remove outstanding orders linked to the removed customer to prevent dangling requests.
                if (RequireBuffer<CWaitingForItem>(belongsToGroup.Group, out DynamicBuffer<CWaitingForItem> waitingForItems))
                {
                    // Scan the pending order list backwards so index removal remains stable.
                    for (int j = waitingForItems.Length - 1; j > -1; j--)
                    {
                        // Ignore orders belonging to other group members.
                        if (waitingForItems[j].MemberIndex != targetedIndex)
                            continue;
                        waitingForItems.RemoveAt(j);
                        break;
                    }
                }
                else
                {
                    DebugLogSystem.LogWarning($"[KillCustomers] Group {belongsToGroup.Group.Index} missing CWaitingForItem buffer; orders may remain for customer {customer.Index}.");
                }
            }

            EntityManager.DestroyEntity(CustomersToKill);
        }

        /// <summary>
        /// Spawns a corpse appliance at the slain customer's position and, when applicable, scatters
        /// blood spills to reinforce the crime scene ambience.
        /// </summary>
        /// <param name="ctx">The entity context used to create temporary entities.</param>
        /// <param name="cPosition">The position of the murdered customer.</param>
        /// <param name="bloody">A value indicating whether the death should produce gore.</param>
        private void CreateCorpse(EntityContext ctx, CPosition cPosition, bool bloody)
        {
            // Creating corpse
            Entity corpse = ctx.CreateEntity();
            int corpseID = GDOUtils.GetCustomGameDataObject<CustomerFloorCorpse>().ID;
            ctx.Set<CCreateAppliance>(corpse, new CCreateAppliance
            {
                ID = corpseID,
                ForceLayer = OccupancyLayer.Ceiling
            });
            ctx.Set<CPosition>(corpse, new CPosition(cPosition.Position, cPosition.Rotation));

            // Skip gore generation when the victim was clean.
            if (!bloody)
            {
                DebugLogSystem.LogVerbose("[KillCustomers] Corpse spawned without blood spills.");
                return;
            }

            // Creating blood spills
            int minbloodSpills = HasStatus((RestaurantStatus)VariousUtils.GetID("messymurder")) ? 1 : 0;
            int maxbloodSpills = HasStatus((RestaurantStatus)VariousUtils.GetID("messymurder")) ? 3 : 2;

            int spillsCreated = 0;

            // Spawn immersive blood spills when the restaurant status warrants additional gore.
            for (int i = 0; i < UnityEngine.Random.Range(minbloodSpills, maxbloodSpills + 1); i++)
            {
                Entity bloodSpill = ctx.CreateEntity();
                ctx.Set<CMessRequest>(bloodSpill, new CMessRequest
                {
                    ID = GDOUtils.GetCustomGameDataObject<BloodSpill1>().ID,
                    OverwriteOtherMesses = false
                });

                // This is so spills don't spawn out of bounds, becoming an uncleanable illegal sight
                // Doesn't work though since mess request creates the mess appliances
                /*if (!TileManager.IsSuitableEmptyTile(cPosition, allow_oob: false, allow_outside: true))
                    continue;*/

                ctx.Set<CPosition>(bloodSpill, new CPosition(cPosition.Position));
                spillsCreated++;
            }

            // Record how many spills were produced to help diagnose performance impact.
            DebugLogSystem.LogVerbose($"[KillCustomers] Generated {spillsCreated} blood spills for corpse at {cPosition.Position}.");
        }
    }
}
