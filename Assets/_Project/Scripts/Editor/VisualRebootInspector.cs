using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.HUD;
using TomorrowsCrime.UI.Menu;
using TomorrowsCrime.UI.Notebook;

namespace TomorrowsCrime.EditorTools
{
    /// <summary>
    /// Automated high-resolution visual inspection tool for Tomorrow's Crime Step 7 Visual Reboot.
    /// Captures multiple camera perspectives of Meridian House, Concierge Pierre, and key props
    /// to disk as 1920x1080 PNG images for genuine visual inspection.
    /// </summary>
    public static class VisualRebootInspector
    {
        private const string ScreenshotsFolder = "Docs/Screenshots";
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";

        [MenuItem("Tomorrow's Crime/Capture Visual Reboot Screenshots", priority = 3)]
        public static void CaptureAllScreenshots()
        {
            if (!Directory.Exists(ScreenshotsFolder))
            {
                Directory.CreateDirectory(ScreenshotsFolder);
            }

            // Ensure prototype scene is built and open
            PrototypeSceneSetup.BuildPrototypeScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int width = 1920;
            int height = 1080;

            // Shot 1: Lobby Architectural Overview from Entrance
            CaptureCameraView(
                "Shot_01_Lobby_Overview.png",
                position: new Vector3(0f, 1.7f, -7.5f),
                rotation: Quaternion.Euler(5f, 0f, 0f),
                fov: 65f,
                width, height
            );

            // Shot 2: Concierge Pierre Stylized Character Model (Medium-Close Portrait over desk)
            CaptureCameraView(
                "Shot_02_Concierge_Pierre_Character.png",
                position: new Vector3(0.4f, 1.55f, 3.2f),
                rotation: Quaternion.Euler(-1f, -8f, 0f),
                fov: 34f,
                width, height
            );

            // Shot 3: Reception Desk & Emerald Banker Lamp
            CaptureCameraView(
                "Shot_03_Reception_Desk_BankerLamp.png",
                position: new Vector3(-1.0f, 1.45f, 2.8f),
                rotation: Quaternion.Euler(12f, 18f, 0f),
                fov: 46f,
                width, height
            );

            // Shot 4: Center Burled Walnut Table & Stamped Gold Keycard
            CaptureCameraView(
                "Shot_04_Table_And_Keycard.png",
                position: new Vector3(0.35f, 1.35f, -4.2f),
                rotation: Quaternion.Euler(22f, -14f, 0f),
                fov: 42f,
                width, height
            );

            // Shot 5: Room 102 Doorway, Sconces & Card Reader
            CaptureCameraView(
                "Shot_05_Room102_Entrance.png",
                position: new Vector3(0f, 1.6f, 8.2f),
                rotation: Quaternion.Euler(4f, 0f, 0f),
                fov: 52f,
                width, height
            );

            Debug.Log($"[Tomorrow's Crime] Visual Reboot screenshots captured successfully to: {ScreenshotsFolder}/");
            AssetDatabase.Refresh();
        }

        private static void CaptureCameraView(string filename, Vector3 position, Quaternion rotation, float fov, int width, int height)
        {
            var camGo = new GameObject("InspectionCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = position;
            cam.transform.rotation = rotation;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = new Color(0.05f, 0.08f, 0.12f); // Meridian Midnight exterior

            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            string outPath = Path.Combine(ScreenshotsFolder, filename);
            File.WriteAllBytes(outPath, bytes);

            cam.targetTexture = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);

            Debug.Log($"[VisualRebootInspector] Saved screenshot: {outPath} ({bytes.Length / 1024} KB)");
        }
    }
}
