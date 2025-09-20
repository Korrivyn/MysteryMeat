using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    [UpdateInGroup(typeof(DestructionGroup)), UpdateAfter(typeof(KillCustomers))]
    public class DestroyEmptyCustomerGroups : DaySystem, IModSystem
    {
        EntityQuery CustomerGroups;
        EntityQuery AlertedDiners;

        protected override void Initialise()
        {
            base.Initialise();

            CustomerGroups = GetEntityQuery(typeof(CCustomerGroup));
            AlertedDiners = GetEntityQuery(new QueryHelper()
                .All(typeof(CAlertedCustomer), typeof(CBelongsToGroup)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> _customerGroups = CustomerGroups.ToEntityArray(Allocator.Temp);
            using NativeArray<CBelongsToGroup> _alertedDiners = AlertedDiners.ToComponentDataArray<CBelongsToGroup>(Allocator.Temp);

            for (int i = _customerGroups.Length - 1; i > -1; i--)
            {
                Entity customerGroup = _customerGroups[i];

                if (RequireBuffer<CGroupMember>(customerGroup, out DynamicBuffer<CGroupMember> groupMembers))
                {
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

                        if (hasAlertedMembersInFlight)
                        {
                            continue;
                        }

                        if (Require<CHasIndicator>(customerGroup, out CHasIndicator cHasIndicator))
                        {
                            EntityManager.DestroyEntity(cHasIndicator.Indicator);
                        }

                        EntityManager.DestroyEntity(customerGroup);
                    }
                }
            }
        }
    }
}
