using Kitchen;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Customs.Appliances;
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
    [UpdateInGroup(typeof(DestructionGroup), OrderFirst = true)]
    public class KillCustomers : DaySystem, IModSystem
    {
        EntityQuery CustomersToKill;
        EntityQuery OrderIndicators;

        protected override void Initialise()
        {
            base.Initialise();
            CustomersToKill = GetEntityQuery(typeof(CCustomer), typeof(CKilled));
            OrderIndicators = GetEntityQuery(typeof(CHasItemCollectionIndicator));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> _customers = CustomersToKill.ToEntityArray(Allocator.Temp);
            using NativeArray<Entity> _orderIndicators = OrderIndicators.ToEntityArray(Allocator.Temp);
            EntityContext ctx = new EntityContext(EntityManager);

            for (int i = 0; i < _customers.Length; i++)
            {
                Entity customer = _customers[i];

                CPosition customerPosition = EntityManager.GetComponentData<CPosition>(customer);
                CKilled cKilled = EntityManager.GetComponentData<CKilled>(customer);

                CreateCorpse(ctx, customerPosition, cKilled.Bloody);

                if (!Require(customer, out CBelongsToGroup belongsToGroup) ||
                    !RequireBuffer(belongsToGroup.Group, out DynamicBuffer<CGroupMember> groupMembers))

                    continue;

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
                if (RequireBuffer<CWaitingForItem>(belongsToGroup.Group, out DynamicBuffer<CWaitingForItem> waitingForItems))
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

            EntityManager.DestroyEntity(CustomersToKill);
        }

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

            if (!bloody)
                return;

            // Creating blood spills
            int minbloodSpills = HasStatus((RestaurantStatus)VariousUtils.GetID("messymurder")) ? 1 : 0;
            int maxbloodSpills = HasStatus((RestaurantStatus)VariousUtils.GetID("messymurder")) ? 3 : 2;

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
            }
        }
    }
}
