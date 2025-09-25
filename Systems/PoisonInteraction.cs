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
        protected override bool IsPossible(ref InteractionData data)
        {
            CItemHolder playerHeldItem;
            CItemHolder applianceHeldItem;

            bool hasPlayerHolder = Require<CItemHolder>(data.Interactor, out playerHeldItem);
            bool hasApplianceHolder = Require<CItemHolder>(data.Target, out applianceHeldItem);

            // Ensure both participants expose holders before evaluating poison flow.
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

                return false;
            }

            bool playerProvidesPoison = Has<CPoisonBottle>(playerHeldItem.HeldItem);
            bool applianceHasEligibleItem = Has<CItem>(applianceHeldItem.HeldItem) && !Has<CPoisoned>(applianceHeldItem.HeldItem);
            bool applianceProvidesPoison = Has<CPoisonBottle>(applianceHeldItem.HeldItem);
            bool playerHasEligibleItem = Has<CItem>(playerHeldItem.HeldItem) && !Has<CPoisoned>(playerHeldItem.HeldItem);

            bool canPlayerPoison = playerProvidesPoison && applianceHasEligibleItem;
            bool canAppliancePoison = applianceProvidesPoison && playerHasEligibleItem;

            DebugLogSystem.LogVerbose(
                $"Poison eligibility evaluated. PlayerProvidesPoison={playerProvidesPoison}, ApplianceProvidesPoison={applianceProvidesPoison}, " +
                $"ApplianceEligible={applianceHasEligibleItem}, PlayerEligible={playerHasEligibleItem}.");

            return canPlayerPoison || canAppliancePoison;
        }

        /// <summary>
        /// Executes the poison transfer so the correct held item gains the poisoned status and plays supporting feedback.
        /// </summary>
        protected override void Perform(ref InteractionData data)
        {
            CItemHolder playerHeldItem;
            CItemHolder applianceHeldItem;

            bool hasPlayerHolder = Require<CItemHolder>(data.Interactor, out playerHeldItem);
            bool hasApplianceHolder = Require<CItemHolder>(data.Target, out applianceHeldItem);

            // Guard: halt execution when either participant lacks the expected holder component.
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
            if (playerProvidesPoison && applianceHasEligibleItem)
            {
                EntityManager.AddComponent<CPoisoned>(applianceHeldItem.HeldItem);
                DebugLogSystem.LogVerbose("Player bottle poisoned the appliance-held item.");
                poisonApplied = true;
            }
            else if (applianceProvidesPoison && playerHasEligibleItem)
            {
                // Apply poison when the appliance donates a bottle to contaminate the player-held item.
                EntityManager.AddComponent<CPoisoned>(playerHeldItem.HeldItem);
                DebugLogSystem.LogVerbose("Appliance bottle poisoned the player-held item.");
                poisonApplied = true;
            }

            // Trigger audio feedback once poison successfully lands on a held item.
            if (poisonApplied)
            {
                CSoundEvent.Create(EntityManager, Mod.PoisonSoundEvent);
            }
            else
            {
                DebugLogSystem.LogVerbose("Poison interaction concluded without applying a status effect.");
            }
        }
    }
}
