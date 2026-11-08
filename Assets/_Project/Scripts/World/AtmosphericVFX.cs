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

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 9.0f;
            main.startSpeed = 0.08f;
            main.startSize = 0.035f;
            main.maxParticles = 180;
            main.startColor = new Color(1.0f, 0.90f, 0.70f, 0.35f); // Soft golden incandescent motes
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 18f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = volumeSize;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.04f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.88f, 0.65f), 0f), new GradientColorKey(new Color(1f, 0.88f, 0.65f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.4f, 0.25f), new GradientAlphaKey(0.4f, 0.75f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.minParticleSize = 0.005f;
            renderer.maxParticleSize = 0.05f;

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
