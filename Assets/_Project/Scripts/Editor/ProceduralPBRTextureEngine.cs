using System.IO;
using UnityEditor;
using UnityEngine;

namespace TomorrowsCrime.EditorTools
{
    /// <summary>
    /// Generates high-fidelity PBR textures with Albedo, Normal Maps, and Metallic-Smoothness masks
    /// to eliminate flat cartoonish graphics and produce a photorealistic 3D game.
    /// </summary>
    public static class ProceduralPBRTextureEngine
    {
        private const string TexturesFolder = "Assets/_Project/Art/Textures";

        [MenuItem("Tomorrow's Crime/Generate High-Fidelity PBR Textures", priority = 10)]
        public static void GenerateAllPBRTextures()
        {
            if (!Directory.Exists(TexturesFolder))
            {
                Directory.CreateDirectory(TexturesFolder);
            }

            Debug.Log("[PBRTextureEngine] Generating high-resolution PBR textures and normal maps...");

            GenerateCheckerMarbleTextures();
            GenerateMahoganyWoodTextures();
            GenerateArtDecoWallpaperTextures();
            GenerateVelvetCarpetTextures();
            GenerateVintageLeatherTextures();
            GenerateBrushedBrassTextures();

            AssetDatabase.Refresh();
            ConfigureTextureImporters();
            AssetDatabase.SaveAssets();

            Debug.Log("[PBRTextureEngine] All photorealistic PBR textures generated and configured successfully.");
        }

        #region Checker Marble
        private static void GenerateCheckerMarbleTextures()
        {
            int size = 1024;
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var height = new float[size, size];
            var smoothness = new Texture2D(size, size, TextureFormat.RGBA32, true);

            int tilesPerSide = 4;
            int tileSize = size / tilesPerSide;
            int groutWidth = 6;

            for (int y = 0; y < size; y++)
            {
                int tileY = y / tileSize;
                int localY = y % tileSize;
                bool edgeY = localY < groutWidth || localY >= (tileSize - groutWidth);
                float distEdgeY = Mathf.Min(localY, tileSize - 1 - localY);

                for (int x = 0; x < size; x++)
                {
                    int tileX = x / tileSize;
                    int localX = x % tileSize;
                    bool edgeX = localX < groutWidth || localX >= (tileSize - groutWidth);
                    float distEdgeX = Mathf.Min(localX, tileSize - 1 - localX);

                    float distEdge = Mathf.Min(distEdgeX, distEdgeY);
                    bool isGrout = edgeX || edgeY;
                    bool isBlackTile = ((tileX + tileY) % 2) == 1;

                    // Marble veining turbulence
                    float nx = x * 0.008f;
                    float ny = y * 0.008f;
                    float veinNoise = Mathf.PerlinNoise(nx * 4f, ny * 4f) * 0.5f +
                                      Mathf.PerlinNoise(nx * 8f, ny * 8f) * 0.25f +
                                      Mathf.PerlinNoise(nx * 16f, ny * 16f) * 0.125f;

                    float marblePerturb = Mathf.Sin((x + y) * 0.035f + veinNoise * 10f);
                    float veinIntensity = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(marblePerturb)), 6f);

                    Color tileColor;
                    float tileSmooth;
                    float hVal;

                    if (isBlackTile)
                    {
                        // Nero Marquina Black Marble (deep charcoal with crisp white/gold calcite veins)
                        Color baseBlack = Color.Lerp(new Color(0.06f, 0.06f, 0.07f), new Color(0.12f, 0.12f, 0.14f), veinNoise);
                        Color veinWhite = new Color(0.88f, 0.86f, 0.80f);
                        tileColor = Color.Lerp(baseBlack, veinWhite, veinIntensity * 0.85f);
                        tileSmooth = 0.94f;
                        hVal = 0.8f + veinIntensity * 0.08f;
                    }
                    else
                    {
                        // Carrara Ivory Marble (warm white with smoky grey and gold veins)
                        Color baseWhite = Color.Lerp(new Color(0.92f, 0.91f, 0.88f), new Color(0.85f, 0.84f, 0.82f), veinNoise * 0.4f);
                        Color veinGrey = new Color(0.42f, 0.40f, 0.38f);
                        tileColor = Color.Lerp(baseWhite, veinGrey, veinIntensity * 0.70f);
                        tileSmooth = 0.92f;
                        hVal = 0.8f - veinIntensity * 0.05f;
                    }

                    // Grout border indentation and bevel
                    if (isGrout)
                    {
                        float groutFactor = Mathf.Clamp01(distEdge / (float)groutWidth);
                        tileColor = Color.Lerp(new Color(0.22f, 0.22f, 0.24f), tileColor, groutFactor);
                        tileSmooth = Mathf.Lerp(0.25f, tileSmooth, groutFactor);
                        hVal = Mathf.Lerp(0.3f, hVal, groutFactor);
                    }

                    albedo.SetPixel(x, y, tileColor);
                    smoothness.SetPixel(x, y, new Color(0.04f, 0.04f, 0.04f, tileSmooth));
                    height[x, y] = hVal;
                }
            }

            albedo.Apply();
            smoothness.Apply();

            SaveTexturePNG(albedo, $"{TexturesFolder}/Floor_CheckerMarble.png");
            SaveTexturePNG(smoothness, $"{TexturesFolder}/Floor_CheckerMarble_Mask.png");

            var normal = GenerateNormalMapFromHeight(height, size, size, 1.2f);
            SaveTexturePNG(normal, $"{TexturesFolder}/Floor_CheckerMarble_Normal.png");
        }
        #endregion

        #region Mahogany Wood
        private static void GenerateMahoganyWoodTextures()
        {
            int size = 1024;
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var height = new float[size, size];
            var smoothness = new Texture2D(size, size, TextureFormat.RGBA32, true);

            for (int y = 0; y < size; y++)
            {
                float ny = y * 0.005f;
                for (int x = 0; x < size; x++)
                {
                    float nx = x * 0.025f;

                    // Wood annual ring fibers and longitudinal pores
                    float fiberWaviness = Mathf.PerlinNoise(nx * 2f, ny * 12f) * 18f;
                    float grainCoord = (x * 0.08f) + fiberWaviness;
                    float grainRing = Mathf.Sin(grainCoord);
                    float fineRays = Mathf.PerlinNoise(nx * 40f, ny * 80f);

                    float darkRing = Mathf.Pow(Mathf.Clamp01(grainRing * 0.5f + 0.5f), 3f);

                    // Deep warm mahogany palette (dark bordeaux brown with amber undertones)
                    Color woodDark = new Color(0.18f, 0.07f, 0.04f);
                    Color woodLight = new Color(0.38f, 0.16f, 0.09f);
                    Color woodAmber = new Color(0.48f, 0.22f, 0.11f);

                    Color col = Color.Lerp(woodDark, woodLight, darkRing);
                    col = Color.Lerp(col, woodAmber, fineRays * 0.25f);

                    float hVal = 0.5f + (darkRing * 0.3f) + (fineRays * 0.15f);
                    float smoothVal = 0.78f + (fineRays * 0.08f); // High-gloss lacquer finish

                    albedo.SetPixel(x, y, col);
                    smoothness.SetPixel(x, y, new Color(0.05f, 0.05f, 0.05f, smoothVal));
                    height[x, y] = hVal;
                }
            }

            albedo.Apply();
            smoothness.Apply();

            SaveTexturePNG(albedo, $"{TexturesFolder}/Wood_PolishedMahogany.png");
            SaveTexturePNG(smoothness, $"{TexturesFolder}/Wood_PolishedMahogany_Mask.png");

            var normal = GenerateNormalMapFromHeight(height, size, size, 1.2f);
            SaveTexturePNG(normal, $"{TexturesFolder}/Wood_PolishedMahogany_Normal.png");
        }
        #endregion

        #region Art Deco Wallpaper
        private static void GenerateArtDecoWallpaperTextures()
        {
            int size = 1024;
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var height = new float[size, size];
            var smoothness = new Texture2D(size, size, TextureFormat.RGBA32, true);

            int motifGrid = 4;
            int cellSize = size / motifGrid;

            for (int y = 0; y < size; y++)
            {
                int cellY = y / cellSize;
                float localY = (float)(y % cellSize) / cellSize;

                for (int x = 0; x < size; x++)
                {
                    int cellX = x / cellSize;
                    float localX = (float)(x % cellSize) / cellSize;

                    // Stagger alternate rows for classic wallpaper repeat
                    if (cellY % 2 == 1)
                    {
                        localX = (localX + 0.5f) % 1.0f;
                    }

                    // Background: Deep Royal Emerald Silk Weave
                    float weave = Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.1f;
                    Color emeraldSilk = new Color(0.08f + weave, 0.18f + weave, 0.13f + weave);

                    // Art Deco Fan / Chevron motif calculation
                    float dx = (localX - 0.5f) * 2f;
                    float dy = localY;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // Stepped fan arches
                    float fanRings = Mathf.Sin(dist * 22f);
                    float fanRays = Mathf.Cos(angle * 7f);
                    bool isFanArch = dist < 0.85f && dist > 0.15f && fanRings > 0.45f && fanRays > -0.2f;

                    // Diamond outline frame
                    float diamond = Mathf.Abs(localX - 0.5f) + Mathf.Abs(localY - 0.5f);
                    bool isDiamondBorder = Mathf.Abs(diamond - 0.48f) < 0.035f;

                    bool isGildedPattern = isFanArch || isDiamondBorder;

                    Color finalCol;
                    float smoothVal;
                    float hVal;

                    if (isGildedPattern)
                    {
                        // Gilded Gold Foil with embossed relief
                        finalCol = new Color(0.88f, 0.74f, 0.32f);
                        smoothVal = 0.82f;
                        hVal = 0.85f;
                    }
                    else
                    {
                        finalCol = emeraldSilk;
                        smoothVal = 0.28f;
                        hVal = 0.35f + weave * 0.5f;
                    }

                    albedo.SetPixel(x, y, finalCol);
                    smoothness.SetPixel(x, y, new Color(isGildedPattern ? 0.75f : 0.08f, 0f, 0f, smoothVal));
                    height[x, y] = hVal;
                }
            }

            albedo.Apply();
            smoothness.Apply();

            SaveTexturePNG(albedo, $"{TexturesFolder}/Wallpaper_ArtDecoDamask.png");
            SaveTexturePNG(smoothness, $"{TexturesFolder}/Wallpaper_ArtDecoDamask_Mask.png");

            var normal = GenerateNormalMapFromHeight(height, size, size, 1.4f);
            SaveTexturePNG(normal, $"{TexturesFolder}/Wallpaper_ArtDecoDamask_Normal.png");
        }
        #endregion

        #region Velvet Carpet
        private static void GenerateVelvetCarpetTextures()
        {
            int size = 1024;
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var height = new float[size, size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Dense textile pile weave micro-noise
                    float pile = Mathf.PerlinNoise(x * 0.4f, y * 0.4f) * 0.12f;

                    // Vintage runner border trim (gold filigree edging along X borders)
                    int borderMargin = 64;
                    bool isBorder = x < borderMargin || x >= (size - borderMargin);
                    bool isBorderPinstripe = (x > (borderMargin - 16) && x < borderMargin) ||
                                             (x > (size - borderMargin) && x < (size - borderMargin + 16));

                    Color col;
                    float hVal;

                    if (isBorderPinstripe)
                    {
                        col = new Color(0.82f, 0.68f, 0.28f); // Gold filigree thread
                        hVal = 0.65f;
                    }
                    else if (isBorder)
                    {
                        col = new Color(0.20f + pile, 0.05f, 0.08f); // Deep dark wine border
                        hVal = 0.5f + pile;
                    }
                    else
                    {
                        col = new Color(0.38f + pile, 0.09f + pile * 0.5f, 0.14f); // Rich Bordeaux velvet
                        hVal = 0.5f + pile;
                    }

                    albedo.SetPixel(x, y, col);
                    height[x, y] = hVal;
                }
            }

            albedo.Apply();
            SaveTexturePNG(albedo, $"{TexturesFolder}/Carpet_VelvetBurgundy.png");

            var normal = GenerateNormalMapFromHeight(height, size, size, 1.0f);
            SaveTexturePNG(normal, $"{TexturesFolder}/Carpet_VelvetBurgundy_Normal.png");
        }
        #endregion

        #region Vintage Leather
        private static void GenerateVintageLeatherTextures()
        {
            int size = 512;
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var height = new float[size, size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Pebble grain leather cell noise
                    float cell1 = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                    float cell2 = Mathf.PerlinNoise(x * 0.35f, y * 0.35f);
                    float leatherGrain = cell1 * 0.7f + cell2 * 0.3f;

                    // Cognac antique leather tones
                    Color darkCrease = new Color(0.18f, 0.10f, 0.06f);
                    Color richLeather = new Color(0.35f, 0.20f, 0.12f);
                    Color highlightPebble = new Color(0.48f, 0.28f, 0.16f);

                    Color col = Color.Lerp(darkCrease, richLeather, leatherGrain);
                    if (leatherGrain > 0.6f)
                    {
                        col = Color.Lerp(col, highlightPebble, (leatherGrain - 0.6f) * 2.5f);
                    }

                    albedo.SetPixel(x, y, col);
                    height[x, y] = leatherGrain;
                }
            }

            albedo.Apply();
            SaveTexturePNG(albedo, $"{TexturesFolder}/Leather_VintageCognac.png");

            var normal = GenerateNormalMapFromHeight(height, size, size, 1.2f);
            SaveTexturePNG(normal, $"{TexturesFolder}/Leather_VintageCognac_Normal.png");
        }
        #endregion

        #region Brushed Brass
        private static void GenerateBrushedBrassTextures()
        {
            int size = 512;
            var height = new float[size, size];

            for (int y = 0; y < size; y++)
            {
                float fineLathe = Mathf.PerlinNoise(y * 0.8f, 0f);
                for (int x = 0; x < size; x++)
                {
                    float microScratch = Mathf.PerlinNoise(x * 0.05f, y * 0.8f);
                    height[x, y] = fineLathe * 0.6f + microScratch * 0.4f;
                }
            }

            var normal = GenerateNormalMapFromHeight(height, size, size, 0.8f);
            SaveTexturePNG(normal, $"{TexturesFolder}/Brass_Brushed_Normal.png");
        }
        #endregion

        #region Normal Map Generation
        private static Texture2D GenerateNormalMapFromHeight(float[,] height, int width, int heightSize, float strength)
        {
            var normalMap = new Texture2D(width, heightSize, TextureFormat.RGBA32, true);

            for (int y = 0; y < heightSize; y++)
            {
                int yPrev = (y - 1 + heightSize) % heightSize;
                int yNext = (y + 1) % heightSize;

                for (int x = 0; x < width; x++)
                {
                    int xPrev = (x - 1 + width) % width;
                    int xNext = (x + 1) % width;

                    // Sobel filter convolution
                    float tl = height[xPrev, yPrev];
                    float l  = height[xPrev, y];
                    float bl = height[xPrev, yNext];
                    float t  = height[x, yPrev];
                    float b  = height[x, yNext];
                    float tr = height[xNext, yPrev];
                    float r  = height[xNext, y];
                    float br = height[xNext, yNext];

                    float dX = (tr + 2f * r + br) - (tl + 2f * l + bl);
                    float dY = (bl + 2f * b + br) - (tl + 2f * t + tr);

                    Vector3 normal = new Vector3(-dX * strength, -dY * strength, 1.0f).normalized;

                    // Map [-1, 1] to [0, 1]
                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f,
                        1.0f
                    );

                    normalMap.SetPixel(x, y, normalColor);
                }
            }

            normalMap.Apply();
            return normalMap;
        }

        private static void SaveTexturePNG(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        private static void ConfigureTextureImporters()
        {
            var normalFiles = Directory.GetFiles(TexturesFolder, "*_Normal.png");
            foreach (var file in normalFiles)
            {
                string assetPath = file.Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.SaveAndReimport();
                }
            }

            var albedoFiles = Directory.GetFiles(TexturesFolder, "*.png");
            foreach (var file in albedoFiles)
            {
                if (file.Contains("_Normal")) continue;
                string assetPath = file.Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.anisoLevel = 16;
                    importer.SaveAndReimport();
                }
            }
        }
        #endregion
    }
}
