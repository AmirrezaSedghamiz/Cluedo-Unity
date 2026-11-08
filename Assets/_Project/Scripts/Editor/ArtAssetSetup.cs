using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TomorrowsCrime.EditorTools
{
    /// <summary>
    /// Automates the creation and configuration of photorealistic PBR Materials, Normal Maps,
    /// and Model Importers for Tomorrow's Crime.
    /// Replaces cartoonish flat shading with high-fidelity URP Lit shaders and micro-surface details.
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

            // 1. Ensure URP and Post-Processing are active
            URPConfigurationHelper.SetupHighFidelityURP();

            // 2. Ensure PBR textures & normal maps exist
            ProceduralPBRTextureEngine.GenerateAllPBRTextures();

            // 3. Create or update rich PBR materials
            var materials = CreateOrUpdateMaterials();

            // 4. Configure 3D Model Importers
            ConfigureModelImporters(materials);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tomorrow's Crime] Photorealistic PBR materials and model importers configured successfully.");
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

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            // Load PBR Textures and Normal Maps
            var floorTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Floor_CheckerMarble.png");
            var floorNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Floor_CheckerMarble_Normal.png");

            var wallpaperTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Wallpaper_ArtDecoDamask.png");
            var wallpaperNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Wallpaper_ArtDecoDamask_Normal.png");

            var woodTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Wood_PolishedMahogany.png");
            var woodNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Wood_PolishedMahogany_Normal.png");

            var carpetTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Carpet_VelvetBurgundy.png");
            var carpetNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Carpet_VelvetBurgundy_Normal.png");

            var leatherTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Leather_VintageCognac.png");
            var leatherNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Leather_VintageCognac_Normal.png");

            var brassNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Brass_Brushed_Normal.png");

            // 1. UniformNavy (Royal Tailored Concierge Wool with subtle sheen)
            dict["UniformNavy"] = GetOrCreateMaterial("M_UniformNavy", shader,
                new Color(0.10f, 0.15f, 0.25f), smoothness: 0.38f, metallic: 0.05f);

            // 2. BrassGold (High-Gloss Polished Antique Brass with micro-scratches)
            dict["BrassGold"] = GetOrCreateMaterial("M_BrassGold", shader,
                new Color(0.88f, 0.74f, 0.28f), smoothness: 0.88f, metallic: 0.94f, normalMap: brassNormal, normalScale: 0.8f);

            // 3. ShirtWhite (Crisp Starched French Cotton)
            dict["ShirtWhite"] = GetOrCreateMaterial("M_ShirtWhite", shader,
                new Color(0.97f, 0.97f, 0.96f), smoothness: 0.22f, metallic: 0.0f);

            // 4. TieBurgundy (Silk Bordeaux Jacquard Crimson)
            dict["TieBurgundy"] = GetOrCreateMaterial("M_TieBurgundy", shader,
                new Color(0.48f, 0.10f, 0.16f), smoothness: 0.65f, metallic: 0.12f);

            // 5. TrousersCharcoal (Formal Wool Charcoal)
            dict["TrousersCharcoal"] = GetOrCreateMaterial("M_TrousersCharcoal", shader,
                new Color(0.13f, 0.14f, 0.17f), smoothness: 0.28f, metallic: 0.0f);

            // 6. ShoesLeather (Mirror-Polished Black Calfskin Oxford)
            dict["ShoesLeather"] = GetOrCreateMaterial("M_ShoesLeather", shader,
                new Color(0.05f, 0.05f, 0.07f), smoothness: 0.92f, metallic: 0.08f, normalMap: leatherNormal, normalScale: 0.5f);

            // 7. SkinStylized -> Photorealistic Skin Tone with warm subsurface scattering aesthetic
            dict["SkinStylized"] = GetOrCreateMaterial("M_SkinStylized", shader,
                new Color(0.92f, 0.78f, 0.70f), smoothness: 0.44f, metallic: 0.0f);

            // 8. HairDark (Natural Brunette Hair)
            dict["HairDark"] = GetOrCreateMaterial("M_HairDark", shader,
                new Color(0.09f, 0.07f, 0.06f), smoothness: 0.52f, metallic: 0.05f);

            // 9. WoodMahogany (Rich Polished Dark Mahogany with Organic Grain & Specular Depth)
            dict["WoodMahogany"] = GetOrCreateMaterial("M_WoodMahogany", shader,
                Color.white, smoothness: 0.82f, metallic: 0.04f, mainTex: woodTex, normalMap: woodNormal, normalScale: 1.2f);

            // 10. WoodWalnut (Burled Walnut Wood)
            dict["WoodWalnut"] = GetOrCreateMaterial("M_WoodWalnut", shader,
                new Color(0.85f, 0.70f, 0.55f), smoothness: 0.76f, metallic: 0.04f, mainTex: woodTex, normalMap: woodNormal, normalScale: 1.0f);

            // 11. MarbleBlack (Polished Nero Marquina Marble with Gold/White Calcite Veining)
            dict["MarbleBlack"] = GetOrCreateMaterial("M_MarbleBlack", shader,
                new Color(0.08f, 0.08f, 0.09f), smoothness: 0.95f, metallic: 0.08f, normalMap: floorNormal, normalScale: 0.6f);

            // 12. MarbleIvory (Polished Calacatta Ivory Marble with Soft Veins)
            dict["MarbleIvory"] = GetOrCreateMaterial("M_MarbleIvory", shader,
                new Color(0.95f, 0.94f, 0.91f), smoothness: 0.94f, metallic: 0.04f, normalMap: floorNormal, normalScale: 0.6f);

            // 13. WallpaperEmerald (Gilded Art Deco Emerald Damask with Embossed 3D Relief)
            dict["WallpaperEmerald"] = GetOrCreateMaterial("M_WallpaperEmerald", shader,
                Color.white, smoothness: 0.38f, metallic: 0.12f, mainTex: wallpaperTex, normalMap: wallpaperNormal, normalScale: 1.4f);

            // 14. FloorCheckerMarble (High-Definition Carrara & Nero Marquina Diamond Checker with Beveled Grout Seams)
            dict["FloorCheckerMarble"] = GetOrCreateMaterial("M_FloorCheckerMarble", shader,
                Color.white, smoothness: 0.94f, metallic: 0.06f, mainTex: floorTex, normalMap: floorNormal, normalScale: 1.3f);

            // 15. CarpetBurgundy (Deep Bordeaux Wool Velvet with Woven Border Filigree)
            dict["CarpetBurgundy"] = GetOrCreateMaterial("M_CarpetBurgundy", shader,
                Color.white, smoothness: 0.16f, metallic: 0.0f, mainTex: carpetTex, normalMap: carpetNormal, normalScale: 1.2f);

            // 15b. CarpetSapphire (Royal Midnight Sapphire Velvet)
            dict["CarpetSapphire"] = GetOrCreateMaterial("M_CarpetSapphire", shader,
                new Color(0.08f, 0.14f, 0.32f), smoothness: 0.16f, metallic: 0.0f, normalMap: carpetNormal, normalScale: 1.0f);

            // 15c. WallpaperNavy (Royal Prussian Blue Damask)
            dict["WallpaperNavy"] = GetOrCreateMaterial("M_WallpaperNavy", shader,
                new Color(0.85f, 0.90f, 1.0f), smoothness: 0.35f, metallic: 0.10f, mainTex: wallpaperTex, normalMap: wallpaperNormal, normalScale: 1.2f);

            // 15d. WallpaperBordeaux (Victorian Crimson Damask)
            dict["WallpaperBordeaux"] = GetOrCreateMaterial("M_WallpaperBordeaux", shader,
                new Color(1.0f, 0.70f, 0.75f), smoothness: 0.35f, metallic: 0.10f, mainTex: wallpaperTex, normalMap: wallpaperNormal, normalScale: 1.2f);

            // 15e. WallpaperArtDecoDamask (Gilded Black & Gold Damask)
            dict["WallpaperArtDecoDamask"] = GetOrCreateMaterial("M_WallpaperArtDecoDamask", shader,
                Color.white, smoothness: 0.40f, metallic: 0.15f, mainTex: wallpaperTex, normalMap: wallpaperNormal, normalScale: 1.4f);

            // 15f. FloorSlate (Charcoal Slate Architectural Tile)
            dict["FloorSlate"] = GetOrCreateMaterial("M_FloorSlate", shader,
                new Color(0.18f, 0.20f, 0.22f), smoothness: 0.42f, metallic: 0.02f);

            // 15g. AgedParchment (Archive Study Parchment with warm aged edge tint)
            dict["AgedParchment"] = GetOrCreateMaterial("M_AgedParchment", shader,
                new Color(0.88f, 0.82f, 0.72f), smoothness: 0.22f, metallic: 0.0f);

            // 15h. VelvetEmerald (Plush Emerald Armchair Upholstery)
            dict["VelvetEmerald"] = GetOrCreateMaterial("M_VelvetEmerald", shader,
                new Color(0.08f, 0.32f, 0.16f), smoothness: 0.24f, metallic: 0.0f, normalMap: carpetNormal, normalScale: 0.8f);

            // 15i. VelvetGold (Gilded Gold Velvet)
            dict["VelvetGold"] = GetOrCreateMaterial("M_VelvetGold", shader,
                new Color(0.82f, 0.65f, 0.22f), smoothness: 0.28f, metallic: 0.05f, normalMap: carpetNormal, normalScale: 0.8f);

            // 15j. GreenFelt (Billiards and Card Table Felt)
            dict["GreenFelt"] = GetOrCreateMaterial("M_GreenFelt", shader,
                new Color(0.10f, 0.46f, 0.22f), smoothness: 0.12f, metallic: 0.0f);

            // 15k. LeatherCognac (Chesterfield Vintage Leather)
            dict["LeatherCognac"] = GetOrCreateMaterial("M_LeatherCognac", shader,
                Color.white, smoothness: 0.62f, metallic: 0.04f, mainTex: leatherTex, normalMap: leatherNormal, normalScale: 1.1f);

            // 16. AmberGlow (Frosted Amber Lamp / Filament with Radiant Emission)
            var amberMat = GetOrCreateMaterial("M_AmberGlow", shader,
                new Color(1.0f, 0.75f, 0.30f), smoothness: 0.65f, metallic: 0.0f);
            amberMat.EnableKeyword("_EMISSION");
            amberMat.SetColor("_EmissionColor", new Color(1.0f, 0.75f, 0.28f) * 2.8f);
            dict["AmberGlow"] = amberMat;

            // 17. EmeraldGreen (Banker Lamp Shade)
            dict["EmeraldGreen"] = GetOrCreateMaterial("M_EmeraldGreen", shader,
                new Color(0.10f, 0.42f, 0.16f), smoothness: 0.90f, metallic: 0.15f);

            return dict;
        }

        private static Material GetOrCreateMaterial(string name, Shader shader, Color color,
            float smoothness = 0.5f, float metallic = 0.0f, Texture2D mainTex = null,
            Texture2D normalMap = null, float normalScale = 1.0f)
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

            // Set URP and Standard properties
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (mainTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
            }

            if (normalMap != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normalMap);
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", normalScale);
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
