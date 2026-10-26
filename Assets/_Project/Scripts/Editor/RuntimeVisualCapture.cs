using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.UI.Core;

namespace TomorrowsCrime.EditorTools
{
    /// <summary>
    /// PlayMode automated visual inspection capture tool.
    /// Captures actual runtime UI screens (Main Menu, HUD, Dossier Workspace, Evidence Modal)
    /// to disk as PNG files for genuine visual inspection.
    /// </summary>
    public static class RuntimeVisualCapture
    {
        private const string ScreenshotsFolder = "Docs/Screenshots";
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";

        [MenuItem("Tomorrow's Crime/Capture Runtime UI Screenshots", priority = 4)]
        public static void RunCaptureBatch()
        {
            if (!Directory.Exists(ScreenshotsFolder))
            {
                Directory.CreateDirectory(ScreenshotsFolder);
            }

            // Ensure prototype scene is built and open
            PrototypeSceneSetup.BuildPrototypeScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Add the capture runner component to a temporary GameObject
            var runnerGo = new GameObject("[VisualCaptureRunner]");
            runnerGo.AddComponent<VisualCaptureRunnerBehaviour>();

            EditorApplication.isPlaying = true;
        }
    }

    public class VisualCaptureRunnerBehaviour : MonoBehaviour
    {
        private int _frame = 0;
        private const string Folder = "Docs/Screenshots";

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            _frame++;
            var uiMgr = UIManager.Instance;
            if (uiMgr == null) return;

            if (_frame == 10)
            {
                // 1. Main Menu View
                uiMgr.SetState(UIState.MainMenu);
            }
            else if (_frame == 25)
            {
                Capture("Shot_06_Main_Menu_View.png");
            }
            else if (_frame == 45)
            {
                // 2. In-Game HUD View
                uiMgr.SetState(UIState.InGameHUD);
                uiMgr.ShowToast("ROOM 102 KEYCARD FOUND", "Standard electronic keycard discovered on central lobby table.", new Color(0.83f, 0.69f, 0.22f), 10f);
            }
            else if (_frame == 65)
            {
                Capture("Shot_07_InGame_HUD_View.png");
            }
            else if (_frame == 85)
            {
                // 3. Investigation Dossier Workspace
                uiMgr.SetState(UIState.InvestigationNotebook);
            }
            else if (_frame == 105)
            {
                Capture("Shot_08_Case_Dossier_Workspace.png");
            }
            else if (_frame == 125)
            {
                // 4. Evidence Inspection Modal
                var keyEvidence = AssetDatabase.LoadAssetAtPath<EvidenceDefinition>("Assets/_Project/Data/Evidence/evidence_room102_keycard.asset");
                if (keyEvidence != null)
                {
                    uiMgr.ShowEvidenceModal(keyEvidence);
                }
            }
            else if (_frame == 145)
            {
                Capture("Shot_09_Evidence_Inspection_Modal.png");
            }
            else if (_frame == 170)
            {
                Debug.Log("[VisualCaptureRunner] All runtime UI captures complete!");
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(0);
#endif
            }
        }

        private void Capture(string filename)
        {
            string fullDir = Path.GetFullPath(Folder);
            if (!Directory.Exists(fullDir)) Directory.CreateDirectory(fullDir);
            string fullPath = Path.Combine(fullDir, filename);
            ScreenCapture.CaptureScreenshot(fullPath);
            Debug.Log($"[VisualCaptureRunner] Captured {fullPath} at frame {_frame}");
        }
    }
}
