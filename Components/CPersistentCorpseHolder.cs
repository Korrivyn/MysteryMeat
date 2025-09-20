using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Marks a holder entity that received temporary preservation from the Persistent Corpses status.
    /// This tag lets cleanup logic identify and revert preservation once the status is removed or
    /// when the holder no longer stores a corpse.
    /// </summary>
    public struct CPersistentCorpseHolder : IModComponent
    {
    }
}
