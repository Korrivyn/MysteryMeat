using KitchenMods;
using KitchenMysteryMeat.Enums;
using Unity.Entities;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Tracks suspicion UI state for customers who witnessed illegal activity.
    /// </summary>
    public struct CSuspicionIndicator : IModComponent
    {
        /// <summary>
        /// Indicates the visual indicator style that should be shown to the player.
        /// </summary>
        public SuspicionIndicatorType IndicatorType;

        /// <summary>
        /// References the illegal entity the customer saw so follow-up systems can resolve it.
        /// </summary>
        public Entity? SeenIllegalThing;

        /// <summary>
        /// Stores the total duration that the suspicion indicator should remain active.
        /// </summary>
        public float TotalTime;

        /// <summary>
        /// Captures the remaining time before the suspicion indicator expires.
        /// </summary>
        public float RemainingTime;
    }
}
