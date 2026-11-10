using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TomorrowsCrime.World
{
    /// <summary>
    /// Spawns cinematic atmospheric visual effects: volumetric dust motes,
    /// radiant chandelier illumination halos, and ensures the global Volume exists.
    /// </summary>
    public static class AtmosphericVFX
    {
        public static GameObject CreateDustMotes(Transform parent, Vector3 position, Vector3 volumeSize)
        {
            var go = new GameObject("Atmospheric_DustMotes");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            // Particles disabled to ensure clean, clear, crisp visuals without floating speckles/glitter
            return go;
        }

        public static Volume EnsureGlobalPostProcessingVolume(Transform parent)
        {
            var existingVol = Object.FindAnyObjectByType<Volume>();
            if (existingVol != null && existingVol.isGlobal) return existingVol;

            var volGo = new GameObject("[PostProcessing_GlobalVolume]");
            if (parent != null) volGo.transform.SetParent(parent);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.weight = 1.0f;

#if UNITY_EDITOR
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/Cinematic_VolumeProfile.asset");
            if (profile != null)
            {
                vol.profile = profile;
            }
#endif

            return vol;
        }
    }
}
