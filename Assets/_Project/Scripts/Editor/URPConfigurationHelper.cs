using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TomorrowsCrime.EditorTools
{
    /// <summary>
    /// Configures high-fidelity Universal Render Pipeline (URP) assets, SSAO,
    /// soft shadows, HDR, and cinematic post-processing to eliminate flat graphics.
    /// </summary>
    public static class URPConfigurationHelper
    {
        private const string SettingsFolder = "Assets/_Project/Settings";
        private const string PipelineAssetPath = "Assets/_Project/Settings/URP_HighFidelity_PipelineAsset.asset";
        private const string RendererDataPath = "Assets/_Project/Settings/URP_HighFidelity_RendererData.asset";
        private const string VolumeProfilePath = "Assets/_Project/Settings/Cinematic_VolumeProfile.asset";

        [MenuItem("Tomorrow's Crime/Setup High-Fidelity URP & Post-Processing", priority = 11)]
        public static UniversalRenderPipelineAsset SetupHighFidelityURP()
        {
            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            Debug.Log("[URPConfiguration] Initializing High-Fidelity Universal Render Pipeline Asset & Renderer...");

            // 1. Create or Load Universal Renderer Data
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererDataPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                var ppData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                if (ppData != null) rendererData.postProcessData = ppData;
                AssetDatabase.CreateAsset(rendererData, RendererDataPath);
            }

            // 2. Add Screen Space Ambient Occlusion (SSAO) for grounded contact shadows
            EnsureSSAOFeature(rendererData);

            // 3. Create or Load Universal Render Pipeline Asset
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urpAsset, PipelineAssetPath);
            }

            // 4. Configure Ultra High-Fidelity Graphics Settings
            ConfigurePipelineFidelity(urpAsset);

            // 5. Assign to GraphicsSettings and QualitySettings
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;

            // Also configure all quality levels to use this pipeline asset
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urpAsset;
            }

            // 6. Create Cinematic Volume Profile
            SetupCinematicVolumeProfile();

            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(urpAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[URPConfiguration] High-Fidelity URP, SSAO, and Cinematic Post-Processing successfully configured!");
            return urpAsset;
        }

        private static void EnsureSSAOFeature(UniversalRendererData rendererData)
        {
            bool hasSSAO = false;
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is ScreenSpaceAmbientOcclusion)
                {
                    hasSSAO = true;
                    break;
                }
            }

            if (!hasSSAO)
            {
                var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "ScreenSpaceAmbientOcclusion";
                AssetDatabase.AddObjectToAsset(ssao, rendererData);
                rendererData.rendererFeatures.Add(ssao);
                rendererData.SetDirty();
                Debug.Log("[URPConfiguration] Added ScreenSpaceAmbientOcclusion feature to forward renderer.");
            }
        }

        private static void ConfigurePipelineFidelity(UniversalRenderPipelineAsset asset)
        {
            var so = new SerializedObject(asset);

            // Enable HDR and Depth/Opaque textures
            var propHDR = so.FindProperty("m_SupportsHDR");
            if (propHDR != null) propHDR.boolValue = true;

            var propDepth = so.FindProperty("m_RequireDepthTexture");
            if (propDepth != null) propDepth.boolValue = true;

            var propOpaque = so.FindProperty("m_RequireOpaqueTexture");
            if (propOpaque != null) propOpaque.boolValue = true;

            // Anti-aliasing (4x MSAA)
            var propMSAA = so.FindProperty("m_MSAA");
            if (propMSAA != null) propMSAA.intValue = 4;

            // Main Light Shadows: 4096 resolution, Soft Shadows
            var propMainLightShadows = so.FindProperty("m_MainLightShadowsSupported");
            if (propMainLightShadows != null) propMainLightShadows.boolValue = true;

            var propMainLightShadowRes = so.FindProperty("m_MainLightShadowmapResolution");
            if (propMainLightShadowRes != null) propMainLightShadowRes.intValue = 4096;

            var propSoftShadows = so.FindProperty("m_SoftShadowsSupported");
            if (propSoftShadows != null) propSoftShadows.boolValue = true;

            // Additional Lights Shadows
            var propAdditionalLightShadows = so.FindProperty("m_AdditionalLightShadowsSupported");
            if (propAdditionalLightShadows != null) propAdditionalLightShadows.boolValue = true;

            var propAddShadowRes = so.FindProperty("m_AdditionalLightsShadowmapResolution");
            if (propAddShadowRes != null) propAddShadowRes.intValue = 2048;

            // Shadow Distance & Cascades
            var propShadowDistance = so.FindProperty("m_ShadowDistance");
            if (propShadowDistance != null) propShadowDistance.floatValue = 50f;

            var propShadowCascades = so.FindProperty("m_ShadowCascadeCount");
            if (propShadowCascades != null) propShadowCascades.intValue = 4;

            // Soft Shadow High Quality
            var propSoftQuality = so.FindProperty("m_SoftShadowQuality");
            if (propSoftQuality != null) propSoftQuality.intValue = 2;

            // Additional Lights Limit & Reflection Probes
            var propAddLimit = so.FindProperty("m_AdditionalLightsPerObjectLimit");
            if (propAddLimit != null) propAddLimit.intValue = 8;

            var propReflBlending = so.FindProperty("m_ReflectionProbeBlending");
            if (propReflBlending != null) propReflBlending.boolValue = true;

            var propReflBox = so.FindProperty("m_ReflectionProbeBoxProjection");
            if (propReflBox != null) propReflBox.boolValue = true;

            var propColorGrading = so.FindProperty("m_ColorGradingMode");
            if (propColorGrading != null) propColorGrading.intValue = 1; // HighDynamicRange

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetOrAddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var comp))
            {
                comp = profile.Add<T>(true);
#if UNITY_EDITOR
                if (comp != null && !AssetDatabase.Contains(comp))
                {
                    AssetDatabase.AddObjectToAsset(comp, profile);
                }
#endif
            }
            return comp;
        }

        public static VolumeProfile SetupCinematicVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            // 1. Tonemapping (ACES Filmic)
            var tonemapping = GetOrAddVolumeComponent<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            // 2. Bloom (Subtle warm tungsten bloom)
            var bloom = GetOrAddVolumeComponent<Bloom>(profile);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.65f);
            bloom.scatter.Override(0.70f);
            bloom.tint.Override(new Color(1.0f, 0.92f, 0.80f));

            // 3. Vignette (Cinematic mystery darkening at frame edges)
            var vignette = GetOrAddVolumeComponent<Vignette>(profile);
            vignette.intensity.Override(0.24f);
            vignette.smoothness.Override(0.35f);
            vignette.color.Override(new Color(0.02f, 0.02f, 0.04f));

            // 4. Color Adjustments (Rich film contrast and subtle saturation)
            var colorAdjustments = GetOrAddVolumeComponent<ColorAdjustments>(profile);
            colorAdjustments.contrast.Override(16f);
            colorAdjustments.saturation.Override(6f);
            colorAdjustments.postExposure.Override(0.12f);

            // 5. White Balance (Warm inviting hotel interior)
            var whiteBalance = GetOrAddVolumeComponent<WhiteBalance>(profile);
            whiteBalance.temperature.Override(6f);
            whiteBalance.tint.Override(1.5f);

            // 6. Film Grain (Analog noir texture)
            var grain = GetOrAddVolumeComponent<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.09f);

            // 7. Motion Blur (Cinematic camera motion fluidity)
            var motionBlur = GetOrAddVolumeComponent<MotionBlur>(profile);
            motionBlur.intensity.Override(0.20f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }
    }
}
