using Kitchen;
using KitchenData;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Triggers the game-over condition when alerted customers successfully flee the restaurant.
    /// </summary>
    public class EscapedGameOver : GameSystemBase, IModSystem
    {
        EntityQuery Customers;
        private ComponentType? ReachedDestinationComponentType;

        /// <summary>
        /// Builds the query that tracks fleeing customers and resolves the destination component metadata.
        /// </summary>
        protected override void Initialise()
        {
            base.Initialise();

            Customers = GetEntityQuery(new QueryHelper()
                            // Watch only the alerted leavers so exiting them triggers the lose condition.
                            .All(typeof(CPosition), typeof(CCustomer), typeof(CCustomerLeaving), typeof(CAlertedCustomer)));

            ReachedDestinationComponentType = TryGetReachedDestinationComponentType();

            // Guard: notify when the destination component could not be located for diagnostic purposes.
            if (!ReachedDestinationComponentType.HasValue)
            {
                DebugLogSystem.LogWarning("Could not resolve CReachedDestination; falling back to distance checks.");
            }
        }

        /// <summary>
        /// Ends the run when an alerted customer reaches the escape boundary.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _customers = Customers.ToEntityArray(Allocator.Temp);

            // Guard: exit when no alerted customers are currently fleeing the restaurant.
            if (_customers.Length == 0)
            {
                return;
            }

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
                    DebugLogSystem.LogWarning($"Triggered game over because alerted customer {customer.Index} exited the restaurant.");
                    break;
                }
            }
        }

        /// <summary>
        /// Locates the destination component type, supporting multiple assembly locations.
        /// </summary>
        private static ComponentType? TryGetReachedDestinationComponentType()
        {
            Type type = Type.GetType("Kitchen.CReachedDestination, KitchenMode")
                ?? Type.GetType("Kitchen.CReachedDestination, KitchenMods")
                ?? typeof(CMoveToLocation).Assembly.GetType("Kitchen.CReachedDestination");

            if (type == null)
            {
                DebugLogSystem.LogWarning("Could not locate Kitchen.CReachedDestination in any known assembly.");
                return null;
            }

            return ComponentType.ReadOnly(type);
        }
    }
}
