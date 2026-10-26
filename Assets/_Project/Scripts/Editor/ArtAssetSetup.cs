using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TomorrowsCrime.EditorTools
{
    /// <summary>
    /// Automates the creation and configuration of PBR Materials and Model Importers
    /// for Tomorrow's Crime Step 7 Visual Reboot (Modern Cluedo Art Direction).
    /// </summary>
    public static class ArtAssetSetup
    {
        private const string MaterialsFolder = "Assets/_Project/Art/Materials";
        private const string TexturesFolder = "Assets/_Project/Art/Textures";
        private const string ModelsFolder = "Assets/_Project/Art/Models";

        [MenuItem("Tomorrow's Crime/Setup Art Materials & Models", priority = 2)]
        public static void SetupAllArtAssets()
        {
            EnsureDirectories();
            var materials = CreateOrUpdateMaterials();
            ConfigureModelImporters(materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tomorrow's Crime] Art materials and model importers configured successfully.");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(MaterialsFolder)) Directory.CreateDirectory(MaterialsFolder);
            if (!Directory.Exists(TexturesFolder)) Directory.CreateDirectory(TexturesFolder);
            if (!Directory.Exists(ModelsFolder)) Directory.CreateDirectory(ModelsFolder);
            AssetDatabase.Refresh();
        }

        public static Dictionary<string, Material> CreateOrUpdateMaterials()
        {
            var dict = new Dictionary<string, Material>();

            // Check if active render pipeline is URP or Standard
            Shader shader = null;
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            // Textures
            var floorTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Floor_CheckerMarble.png");
            var wallpaperTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Wallpaper_ArtDecoDamask.png");
            var woodTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Wood_PolishedMahogany.png");

            // 1. UniformNavy (Royal Concierge Blue)
            dict["UniformNavy"] = GetOrCreateMaterial("M_UniformNavy", shader,
                new Color(0.11f, 0.18f, 0.29f), smoothness: 0.35f, metallic: 0.05f);

            // 2. BrassGold (Polished Antique Brass)
            dict["BrassGold"] = GetOrCreateMaterial("M_BrassGold", shader,
                new Color(0.83f, 0.69f, 0.22f), smoothness: 0.85f, metallic: 0.90f);

            // 3. ShirtWhite (Crisp White Cotton)
            dict["ShirtWhite"] = GetOrCreateMaterial("M_ShirtWhite", shader,
                new Color(0.96f, 0.96f, 0.95f), smoothness: 0.20f, metallic: 0.0f);

            // 4. TieBurgundy (Silk Bordeaux Crimson)
            dict["TieBurgundy"] = GetOrCreateMaterial("M_TieBurgundy", shader,
                new Color(0.42f, 0.11f, 0.18f), smoothness: 0.60f, metallic: 0.15f);

            // 5. TrousersCharcoal (Formal Dark Charcoal)
            dict["TrousersCharcoal"] = GetOrCreateMaterial("M_TrousersCharcoal", shader,
                new Color(0.12f, 0.14f, 0.18f), smoothness: 0.25f, metallic: 0.0f);

            // 6. ShoesLeather (Polished Black Oxford)
            dict["ShoesLeather"] = GetOrCreateMaterial("M_ShoesLeather", shader,
                new Color(0.07f, 0.07f, 0.09f), smoothness: 0.88f, metallic: 0.10f);

            // 7. SkinStylized (Warm Stylized Peach)
            dict["SkinStylized"] = GetOrCreateMaterial("M_SkinStylized", shader,
                new Color(0.88f, 0.70f, 0.60f), smoothness: 0.35f, metallic: 0.0f);

            // 8. HairDark (Dark Styled Hair)
            dict["HairDark"] = GetOrCreateMaterial("M_HairDark", shader,
                new Color(0.11f, 0.09f, 0.08f), smoothness: 0.45f, metallic: 0.0f);

            // 9. WoodMahogany (Polished Dark Mahogany)
            dict["WoodMahogany"] = GetOrCreateMaterial("M_WoodMahogany", shader,
                new Color(0.20f, 0.10f, 0.07f), smoothness: 0.72f, metallic: 0.05f, mainTex: woodTex);

            // 10. WoodWalnut (Burled Walnut)
            dict["WoodWalnut"] = GetOrCreateMaterial("M_WoodWalnut", shader,
                new Color(0.25f, 0.15f, 0.10f), smoothness: 0.68f, metallic: 0.05f);

            // 11. MarbleBlack (Nero Marquina Marble)
            dict["MarbleBlack"] = GetOrCreateMaterial("M_MarbleBlack", shader,
                new Color(0.08f, 0.09f, 0.10f), smoothness: 0.92f, metallic: 0.10f);

            // 12. MarbleIvory (Calacatta Ivory Marble)
            dict["MarbleIvory"] = GetOrCreateMaterial("M_MarbleIvory", shader,
                new Color(0.93f, 0.91f, 0.88f), smoothness: 0.90f, metallic: 0.05f);

            // 13. WallpaperEmerald (Art Deco Emerald Damask)
            dict["WallpaperEmerald"] = GetOrCreateMaterial("M_WallpaperEmerald", shader,
                new Color(0.10f, 0.22f, 0.17f), smoothness: 0.30f, metallic: 0.10f, mainTex: wallpaperTex);

            // 14. FloorCheckerMarble (Diamond Checker Marble)
            dict["FloorCheckerMarble"] = GetOrCreateMaterial("M_FloorCheckerMarble", shader,
                Color.white, smoothness: 0.90f, metallic: 0.05f, mainTex: floorTex);

            // 15. CarpetBurgundy (Bordeaux Velvet Runner)
            dict["CarpetBurgundy"] = GetOrCreateMaterial("M_CarpetBurgundy", shader,
                new Color(0.35f, 0.09f, 0.14f), smoothness: 0.15f, metallic: 0.0f);

            // 16. AmberGlow (Frosted Amber Lamp / Flame)
            var amberMat = GetOrCreateMaterial("M_AmberGlow", shader,
                new Color(1.0f, 0.72f, 0.25f), smoothness: 0.50f, metallic: 0.0f);
            amberMat.EnableKeyword("_EMISSION");
            amberMat.SetColor("_EmissionColor", new Color(1.0f, 0.75f, 0.25f) * 2.2f);
            dict["AmberGlow"] = amberMat;

            // 17. EmeraldGreen (Banker Lamp Shade)
            dict["EmeraldGreen"] = GetOrCreateMaterial("M_EmeraldGreen", shader,
                new Color(0.11f, 0.38f, 0.14f), smoothness: 0.85f, metallic: 0.10f);

            return dict;
        }

        private static Material GetOrCreateMaterial(string name, Shader shader, Color color,
            float smoothness = 0.5f, float metallic = 0.0f, Texture2D mainTex = null)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            // Set both Standard and URP properties for complete compatibility
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (mainTex != null)
            {
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureModelImporters(Dictionary<string, Material> materials)
        {
            var modelFiles = Directory.GetFiles(ModelsFolder, "*.obj");
            foreach (var file in modelFiles)
            {
                string assetPath = file.Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer != null)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
