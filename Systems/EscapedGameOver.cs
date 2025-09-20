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
    public class EscapedGameOver : GameSystemBase, IModSystem
    {
        EntityQuery Customers;
        private ComponentType? ReachedDestinationComponentType;

        protected override void Initialise()
        {
            base.Initialise();

            Customers = GetEntityQuery(new QueryHelper()
                            // Watch only the alerted leavers so exiting them triggers the lose condition.
                            .All(typeof(CPosition), typeof(CCustomer), typeof(CCustomerLeaving), typeof(CAlertedCustomer)));

            ReachedDestinationComponentType = TryGetReachedDestinationComponentType();
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> _customers = Customers.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < _customers.Length; i++)
            {
                Entity customer = _customers[i];

                bool hasReachedDestination = false;
                if (ReachedDestinationComponentType.HasValue)
                {
                    hasReachedDestination = EntityManager.HasComponent(customer, ReachedDestinationComponentType.Value);
                }
                else
                {
                    CPosition cPosition = EntityManager.GetComponentData<CPosition>(customer);
                    Vector3 leftRestaurantMoveTarget = new Vector3(-15f, 0f, 0f);
                    hasReachedDestination = Vector3.Magnitude(leftRestaurantMoveTarget - (Vector3)cPosition) < 1f;
                }

                if (hasReachedDestination)
                {
                    // End game if exited
                    EntityManager.CreateEntity(typeof(CLoseLifeEvent));
                    EntityManager.DestroyEntity(customer);
                    break;
                }
            }
        }

        private static ComponentType? TryGetReachedDestinationComponentType()
        {
            Type type = Type.GetType("Kitchen.CReachedDestination, KitchenMode")
                ?? Type.GetType("Kitchen.CReachedDestination, KitchenMods")
                ?? typeof(CMoveToLocation).Assembly.GetType("Kitchen.CReachedDestination");

            if (type == null)
            {
                return null;
            }

            return ComponentType.ReadOnly(type);
        }
    }
}
