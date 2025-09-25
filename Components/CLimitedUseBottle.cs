using KitchenData;
using KitchenMods;
using Unity.Entities;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Tracks refill charges for bottles that can only dispense a limited number of servings before running dry.
    /// </summary>
    public struct CLimitedUseBottle : IModComponent, IItemProperty, IAttachableProperty
    {
        /// <summary>
        /// Defines how many servings the bottle can provide before it must be refilled.
        /// </summary>
        public int Limit;

        /// <summary>
        /// Stores the remaining number of servings currently available from the bottle.
        /// </summary>
        public int FillAmount;

        /// <summary>
        /// Remembers the last customer who interacted with the bottle so suspicion logic can react accordingly.
        /// </summary>
        public Entity LastUsedByCustomer;

        /// <summary>
        /// Provides the item ID to spawn when the bottle has been depleted and should be replaced with an empty container.
        /// </summary>
        public int EmptyBottleID;
    }
}
