using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using System.Text;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Equips customers with interaction and suspicion data so Mystery Meat gameplay systems can
    /// drive bespoke behaviour and suspicion tracking.
    /// </summary>
    public class AddComponentsToCustomers : GameSystemBase, IModSystem
    {
        private EntityQuery CustomersWithoutInteractive;
        private EntityQuery CustomersWithoutSuspicionIndicator;
        /// <summary>
        /// Builds queries that locate customers missing interactive or suspicion components for
        /// later enrichment during updates.
        /// </summary>
        protected override void Initialise()
        {
            CustomersWithoutInteractive = GetEntityQuery(new QueryHelper()
                            .All(typeof(CCustomer))
                            .None(
                                typeof(CIsInteractive)
                            ));

            CustomersWithoutSuspicionIndicator = GetEntityQuery(new QueryHelper()
                            .All(typeof(CCustomer))
                            .None(
                                typeof(CSuspicionIndicator)
                            ));
        }

        /// <summary>
        /// Adds the necessary interaction and suspicion components to customers while emitting
        /// diagnostics about the affected entities.
        /// </summary>
        protected override void OnUpdate()
        {
            // Gather customers that need the interactive component so they respond to manual inputs.
            using NativeArray<Entity> customersNeedingInteractive = CustomersWithoutInteractive.ToEntityArray(Allocator.TempJob);

            // Guard: log a warning when no customers were found even though the system expects pending arrivals.
            if (customersNeedingInteractive.Length == 0)
            {
                DebugLogSystem.LogWarning("AddComponentsToCustomers found no customers requiring CIsInteractive despite expecting pending assignments.");
            }
            else
            {
                // Add the interactive component so customers can be targeted by custom interactions.
                EntityManager.AddComponent<CIsInteractive>(CustomersWithoutInteractive);
                DebugLogSystem.LogVerbose($"Added CIsInteractive to {customersNeedingInteractive.Length} customers to enable interaction handling.");

                // Emit verbose diagnostics enumerating the customers that received the interactive component.
                StringBuilder interactiveCustomersBuilder = new StringBuilder();
                for (int i = 0; i < customersNeedingInteractive.Length; i++)
                {
                    if (interactiveCustomersBuilder.Length > 0)
                    {
                        interactiveCustomersBuilder.Append(", ");
                    }

                    interactiveCustomersBuilder.Append(customersNeedingInteractive[i].Index);
                }

                DebugLogSystem.LogVerbose($"Interactive component applied to customers: {interactiveCustomersBuilder}");
            }

            // Collect customers lacking suspicion indicators so they can be tracked for suspicious actions.
            using NativeArray<Entity> customersNeedingSuspicionIndicator = CustomersWithoutSuspicionIndicator.ToEntityArray(Allocator.TempJob);

            // Detect the cautious crowd status to determine the appropriate suspicion timer duration.
            bool hasCautiousCrowdStatus = HasStatus((RestaurantStatus)VariousUtils.GetID("cautiouscrowd"));

            // Adjust suspicion timing so cautious crowd customers escalate faster than normal patrons.
            float totalTime = hasCautiousCrowdStatus ? 1.0f : 2.0f;

            // Guard: log a warning when no customers were found even though indicators are expected during service.
            if (customersNeedingSuspicionIndicator.Length == 0)
            {
                DebugLogSystem.LogWarning("AddComponentsToCustomers found no customers requiring CSuspicionIndicator despite expecting customer tracking.");
            }
            else
            {
                // Emit verbose diagnostics enumerating the customers that received suspicion indicators.
                StringBuilder suspicionCustomersBuilder = new StringBuilder();

                // Apply suspicion indicator data to each affected customer so suspicion drains over the configured duration.
                for (int i = 0; i < customersNeedingSuspicionIndicator.Length; i++)
                {
                    Entity customer = customersNeedingSuspicionIndicator[i];
                    EntityManager.AddComponentData(customer, new CSuspicionIndicator()
                    {
                        IndicatorType = Enums.SuspicionIndicatorType.Suspicious,
                        TotalTime = totalTime,
                        RemainingTime = totalTime,
                    });

                    if (suspicionCustomersBuilder.Length > 0)
                    {
                        suspicionCustomersBuilder.Append(", ");
                    }

                    suspicionCustomersBuilder.Append(customer.Index);
                }

                DebugLogSystem.LogVerbose($"Added CSuspicionIndicator to {customersNeedingSuspicionIndicator.Length} customers with a total time of {totalTime:0.##} seconds.");
                DebugLogSystem.LogVerbose($"Suspicion indicator applied to customers: {suspicionCustomersBuilder}");
            }
        }
    }
}
