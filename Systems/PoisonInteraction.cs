using System;
using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Coordinates poison bottle exchanges so players can contaminate held items when interacting with appliances.
    /// </summary>
    public class PoisonInteraction : ItemInteractionSystem, IModSystem
    {
        protected override InteractionType RequiredType => InteractionType.Grab;

        /// <summary>
        /// Determines whether the interaction can proceed so that a poison bottle can taint an opposing held item.
        /// </summary>
        // Determines whether any participant can trade poison to contaminate the opposing held item.
        protected override bool IsPossible(ref InteractionData data)
        {
            CItemHolder playerHeldItem;
            CItemHolder applianceHeldItem;

            bool hasPlayerHolder = Require<CItemHolder>(data.Interactor, out playerHeldItem);
            bool hasApplianceHolder = Require<CItemHolder>(data.Target, out applianceHeldItem);

            // Warn when either side lacks the expected holder component for the exchange.
            if (!hasPlayerHolder || !hasApplianceHolder)
            {
                if (!hasPlayerHolder)
                {
                    DebugLogSystem.LogWarning("Player holder missing while evaluating poison swap.");
                }

                if (!hasApplianceHolder)
                {
                    DebugLogSystem.LogWarning("Appliance holder missing while evaluating poison swap.");
                }
            }

            bool playerProvidesPoison = hasPlayerHolder && Has<CPoisonBottle>(playerHeldItem.HeldItem);
            bool applianceHasEligibleItem = hasApplianceHolder && Has<CItem>(applianceHeldItem.HeldItem) && !Has<CPoisoned>(applianceHeldItem.HeldItem);
            bool applianceProvidesPoison = hasApplianceHolder && Has<CPoisonBottle>(applianceHeldItem.HeldItem);
            bool playerHasEligibleItem = hasPlayerHolder && Has<CItem>(playerHeldItem.HeldItem) && !Has<CPoisoned>(playerHeldItem.HeldItem);

            bool isPossible = false;

            // Evaluate whether the player can poison the appliance-held item via their bottle.
            DebugLogSystem.LogVerbose("Checking player-supplied poison path.");
            if (playerProvidesPoison && applianceHasEligibleItem)
            {
                DebugLogSystem.LogVerbose("Player bottle can contaminate the appliance-held item.");
                isPossible = true;
            }

            // Evaluate whether the appliance can poison the player-held item via its bottle.
            DebugLogSystem.LogVerbose("Checking appliance-supplied poison path.");
            if (!isPossible && applianceProvidesPoison && playerHasEligibleItem)
            {
                DebugLogSystem.LogVerbose("Appliance bottle can contaminate the player-held item.");
                isPossible = true;
            }

            return isPossible;
        }

        /// <summary>
        /// Executes the poison transfer so the correct held item gains the poisoned status and plays supporting feedback.
        /// </summary>
        // Applies poison to whichever held item should receive contamination following the bottle exchange.
        protected override void Perform(ref InteractionData data)
        {
            CItemHolder playerHeldItem;
            CItemHolder applianceHeldItem;

            bool hasPlayerHolder = Require<CItemHolder>(data.Interactor, out playerHeldItem);
            bool hasApplianceHolder = Require<CItemHolder>(data.Target, out applianceHeldItem);

            // Halt when the required holders are missing so the exchange cannot proceed.
            if (!hasPlayerHolder || !hasApplianceHolder)
            {
                if (!hasPlayerHolder)
                {
                    DebugLogSystem.LogWarning("Player holder missing while attempting poison swap.");
                }

                if (!hasApplianceHolder)
                {
                    DebugLogSystem.LogWarning("Appliance holder missing while attempting poison swap.");
                }

                return;
            }

            bool playerProvidesPoison = Has<CPoisonBottle>(playerHeldItem.HeldItem);
            bool applianceHasEligibleItem = Has<CItem>(applianceHeldItem.HeldItem) && !Has<CPoisoned>(applianceHeldItem.HeldItem);
            bool applianceProvidesPoison = Has<CPoisonBottle>(applianceHeldItem.HeldItem);
            bool playerHasEligibleItem = Has<CItem>(playerHeldItem.HeldItem) && !Has<CPoisoned>(playerHeldItem.HeldItem);

            bool poisonApplied = false;

            // Apply poison when the player donates a bottle to contaminate the appliance-held item.
            DebugLogSystem.LogVerbose("Evaluating player-supplied poison application.");
            if (playerProvidesPoison && applianceHasEligibleItem)
            {
                EntityManager.AddComponent<CPoisoned>(applianceHeldItem.HeldItem);
                DebugLogSystem.LogVerbose("Player bottle poisoned the appliance-held item.");
                DebugLogSystem.LogVerbose("Poison status applied to the appliance-held item.");
                poisonApplied = true;
            }

            // Apply poison when the appliance donates a bottle to contaminate the player-held item.
            DebugLogSystem.LogVerbose("Evaluating appliance-supplied poison application.");
            if (!poisonApplied && applianceProvidesPoison && playerHasEligibleItem)
            {
                EntityManager.AddComponent<CPoisoned>(playerHeldItem.HeldItem);
                DebugLogSystem.LogVerbose("Appliance bottle poisoned the player-held item.");
                DebugLogSystem.LogVerbose("Poison status applied to the player-held item.");
                poisonApplied = true;
            }

            // Trigger audio feedback once poison successfully lands on a held item.
            if (poisonApplied)
            {
                CSoundEvent.Create(EntityManager, Mod.PoisonSoundEvent);
            }
        }
    }
}
