using Kitchen;
using KitchenData;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Enables tools that kill customers to apply the death state through player interactions.
    /// </summary>
    [UpdateInGroup(typeof(LowPriorityInteractionGroup), OrderFirst = true)]
    public class KillInteraction : ItemInteractionSystem, IModSystem
    {
        protected override bool RequireHold => true;

        protected override bool RequirePress => false;

        /// <summary>
        /// Determines whether the kill interaction is valid for the current participants.
        /// </summary>
        protected override bool IsPossible(ref InteractionData data)
        {
            CToolUser ctoolUser;

            bool canKill = Has<CCustomer>(data.Target) && Require<CToolUser>(data.Interactor, out ctoolUser) && Has<CKillsCustomer>(ctoolUser.CurrentTool) && !Has<CKilled>(data.Target);

            // Guard: emit diagnostics when the interaction cannot proceed.
            if (!canKill)
            {
                DebugLogSystem.LogVerbose("KillInteraction deemed interaction impossible due to missing components or existing CKilled state.");
            }

            return canKill;
        }

        /// <summary>
        /// Applies the kill effect, tagging the target and playing the configured stab sound.
        /// </summary>
        protected override void Perform(ref InteractionData data)
        {
            CSoundEvent.Create(EntityManager, Mod.StabSoundEvent);
            EntityManager.AddComponentData<CKilled>(data.Target, new CKilled() { Bloody = true });
            DebugLogSystem.LogVerbose($"KillInteraction marked customer {data.Target.Index} as killed via interaction.");
        }
    }
}
