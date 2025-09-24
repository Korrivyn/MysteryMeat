using Kitchen.Components;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.MonoBehaviours
{
    /// <summary>
    /// Applies preference-driven volume multipliers to attached sound sources at runtime.
    /// </summary>
    public class PreferenceVolumeAdjuster : MonoBehaviour
    {
        public string PreferenceID = string.Empty;

        /// <summary>
        /// Adjusts the associated sound source volume each frame using the configured preference value.
        /// </summary>
        public void Update()
        {
            // Guard: only attempt to adjust audio when a preference identifier has been assigned.
            if (!string.IsNullOrWhiteSpace(PreferenceID))
            {
                // Guard: obtain the sound source component before mutating its volume multiplier.
                if (TryGetComponent(out SoundSource soundSource))
                {
                    // Guard: avoid dereferencing the preference manager before it has been initialised.
                    if (Mod.PrefManager == null)
                    {
                        if (!_missingPreferenceManagerLogged)
                        {
                            DebugLogSystem.LogVerbose("Deferred volume adjustment because preferences are not yet initialised.");
                            _missingPreferenceManagerLogged = true;
                        }

                        soundSource.VolumeMultiplier = 1.0f;
                    }
                    else
                    {
                        // Reset the log flag after the preference manager becomes available.
                        _missingPreferenceManagerLogged = false;

                        // Guard: clamp the retrieved value to prevent invalid multipliers from preferences.
                        float configuredVolume = Mathf.Clamp(Mod.PrefManager.Get<int>(PreferenceID), 0, 100) / 100.0f;
                        soundSource.VolumeMultiplier = configuredVolume;
                    }
                }
            }
        }

        /// <summary>
        /// Tracks whether the missing preference manager warning has already been emitted.
        /// </summary>
        private static bool _missingPreferenceManagerLogged;
    }
}
