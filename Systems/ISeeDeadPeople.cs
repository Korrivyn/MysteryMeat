using Kitchen;
using KitchenMods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.UI;
using UnityEngine;
using KitchenMysteryMeat.Components;

namespace KitchenMysteryMeat.Systems
{
    public class ISeeDeadPeople : GenericSystemBase, IModSystem
    {
        EntityQuery Customers;
        EntityQuery IllegalEntities;

        protected override void Initialise()
        {
            base.Initialise();

            Customers = GetEntityQuery(new QueryHelper()
                            .All(typeof(CCustomer), typeof(CPosition), typeof(CSuspicionIndicator), typeof(CBelongsToGroup))
                            .None(
                                typeof(CRunningAway)
                            ));
            IllegalEntities = GetEntityQuery(new QueryHelper()
                            .All(typeof(CIllegalSight)));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> _customers = Customers.ToEntityArray(Allocator.Temp);
            using NativeArray<Entity> _illegalEntities = IllegalEntities.ToEntityArray(Allocator.Temp);

            for (int i = _customers.Length - 1; i > -1; i--)
            {
                Entity customer = _customers[i];
                CPosition customerPosition = GetComponent<CPosition>(customer);
                CSuspicionIndicator cSuspicionIndicator = GetComponent<CSuspicionIndicator>(customer);


                foreach (Entity illegalEntity in _illegalEntities)
                {
                    CPosition illegalEntityPos;
                    if (!Require<CPosition>(illegalEntity, out illegalEntityPos))
                    {
                        if (Require<CHeldBy>(illegalEntity, out CHeldBy cHeldBy) && !Require<CPosition>(cHeldBy.Holder, out illegalEntityPos))
                            continue;
                    }

                    // Checking if illegal entity is in customer's view

                    Vector3 vector = illegalEntityPos.Position - customerPosition.Position;
                    float detectionDistance = 10f;
                    // This is so they can only see the illegal entity if it is in the same room.
                    if (vector.sqrMagnitude < detectionDistance && TileManager.GetRoom(illegalEntityPos) == TileManager.GetRoom(customerPosition))
                    {
                        Vector3 rhs = customerPosition.Forward(1f);
                        if (Vector3.Dot(vector.normalized, rhs) > 1f - Mathf.Cos((float)Math.PI / 6f))
                        {
                            // Run away!
                            cSuspicionIndicator.SeenIllegalThing = illegalEntity;
                            EntityManager.SetComponentData<CSuspicionIndicator>(customer, cSuspicionIndicator);
                            continue;
                        }
                    }

                    if (cSuspicionIndicator.SeenIllegalThing == illegalEntity)
                    {
                        cSuspicionIndicator.SeenIllegalThing = null;
                        EntityManager.SetComponentData<CSuspicionIndicator>(customer, cSuspicionIndicator);
                    }

                }
            }
        }
    }
}
