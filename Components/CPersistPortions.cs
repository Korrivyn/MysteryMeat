using KitchenData;
using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Keeps track of consumable portion counts that should persist between days.
    /// </summary>
    public struct CPersistPortions : IModComponent, IItemProperty, IAttachableProperty
    {
        /// <summary>
        /// Indicates how many servings remain available after the latest use.
        /// </summary>
        public int RemainingCount;

        /// <summary>
        /// Stores the total number of servings the item contained at the start of the day for reset logic.
        /// </summary>
        public int TotalCount;
    }
}
