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

        protected override void Initialise()
        {
            base.Initialise();

            Customers = GetEntityQuery(new QueryHelper()
                            .All(typeof(CPosition), typeof(CRunningAway), typeof(CCustomer)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> _customers = Customers.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < _customers.Length; i++)
            {
                Entity customer = _customers[i];

                CPosition cPosition = EntityManager.GetComponentData<CPosition>(customer);

                Vector3 leftRestaurantMoveTarget = new Vector3(-15f, 0f, 0f);
                if (Vector3.Magnitude((Vector3)leftRestaurantMoveTarget - (Vector3)cPosition) < 1f)
                {
                    // End game if exited
                    EntityManager.CreateEntity(typeof(CLoseLifeEvent));
                    EntityManager.DestroyEntity(customer);
                    break;
                }
            }
        }
    }
}
