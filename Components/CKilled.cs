using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Tracks whether a customer has been killed and whether the resulting mess should be treated as bloody.
    /// </summary>
    public struct CKilled : IModComponent
    {
        /// <summary>
        /// Records whether the eliminated customer produced a bloodied body for later cleanup routines.
        /// </summary>
        public bool Bloody;
    }
}
