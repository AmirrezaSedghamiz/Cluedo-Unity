using System;
using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Audio;
using TomorrowsCrime.Core;
using TomorrowsCrime.Events;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Procedural.Hotel;
using TomorrowsCrime.Procedural.NPCs;
using TomorrowsCrime.Procedural.Objects;
using TomorrowsCrime.Procedural.Mystery;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;

namespace TomorrowsCrime.Procedural.Core
{
    public enum SessionGameMode
    {
        ProceduralHotelCase,
        ClassicLobbyPrototype
    }

    /// <summary>
    /// Master investigation session coordinator.
    /// Bridges the procedural generation pipeline with runtime gameplay,
    /// instantiating multi-floor hotel architecture, populating clue items,
    /// binding NPC ecosystem routines, and initializing ground truth for accusation resolution.
    /// </summary>
    public class GameSessionManager : MonoBehaviour, ILoopResetReceiver
    {
        private static GameSessionManager _instance;
        public static GameSessionManager Instance => _instance;

        [Header("Mode Configuration")]
        [SerializeField] private SessionGameMode activeMode = SessionGameMode.ProceduralHotelCase;

        [Header("Active Case State")]
        [SerializeField] private string activeSeedCode = "TC-1A2B3C4D";
        private GeneratedInvestigation _currentInvestigation;
        private GameObject _spawnedWorldRoot;

        public GeneratedInvestigation CurrentInvestigation => _currentInvestigation;
        public string ActiveSeedCode => activeSeedCode;
        public SessionGameMode ActiveMode => activeMode;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }

            // If starting in procedural mode, automatically initialize baseline case
            if (activeMode == SessionGameMode.ProceduralHotelCase && _currentInvestigation == null)
            {
                var seed = InvestigationSeed.FromCode(activeSeedCode);
                InitializeInvestigation(seed, false);
            }
        }

        private void OnDestroy()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.UnregisterResetReceiver(this);
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void StartNewProceduralCase(InvestigationSeed specificSeed = null)
        {
            var seed = specificSeed ?? InvestigationSeed.GenerateRandom();
            activeSeedCode = seed.SeedCode;
            activeMode = SessionGameMode.ProceduralHotelCase;

            InitializeInvestigation(seed, true);
        }

        public void InitializeInvestigation(InvestigationSeed seed, bool playFanfare = true)
        {
            if (seed == null) seed = InvestigationSeed.GenerateRandom();
            activeSeedCode = seed.SeedCode;

            Debug.Log($"[GameSessionManager] Initializing investigation with Seed '{seed.SeedCode}'...");

            // 1. Generate Investigation Data Payload
            _currentInvestigation = GenerationPipeline.GenerateInvestigation(seed);

            // 2. Setup Ground Truth in CaseAccusationService
            if (CaseAccusationService.Instance != null)
            {
                CaseAccusationService.Instance.SetGroundTruth(_currentInvestigation.GroundTruth);
            }

            // 3. Bind Causal Events to Event Engine
            var compiledEvents = ScenarioEventBridge.CompileScenarioEvents(_currentInvestigation.GroundTruth);
            var eventScheduler = FindAnyObjectByType<CausalEventScheduler>();
            if (eventScheduler != null)
            {
                eventScheduler.ClearScheduledEvents();
                foreach (var evt in compiledEvents)
                {
                    eventScheduler.ScheduleEvent(evt);
                }
                Debug.Log($"[GameSessionManager] Bound {compiledEvents.Count} procedural scenario events to scheduler.");
            }

            // 4. Update Developer Inspector UI if present
            var inspector = TomorrowsCrime.Procedural.Debugging.GenerationInspectorUI.Instance;
            if (inspector != null)
            {
                inspector.SetInvestigation(_currentInvestigation);
            }

            // 5. Build physical 4-floor procedural hotel environment
            if (activeMode == SessionGameMode.ProceduralHotelCase)
            {
                if (_spawnedWorldRoot != null)
                {
                    Destroy(_spawnedWorldRoot);
                }

                // Deactivate static prototype lobby if present
                var staticLobby = GameObject.Find("[Environment_HotelLobby]");
                if (staticLobby != null)
                {
                    staticLobby.SetActive(false);
                }

                var mats = ResolveMaterials();
                _spawnedWorldRoot = GenerationPipeline.BuildPlayableWorld(_currentInvestigation, null, mats);

                // Position player at ground lobby spawn
                var player = FindAnyObjectByType<Player.PlayerController>();
                if (player != null)
                {
                    var cc = player.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    player.transform.position = new Vector3(0f, 0.5f, -4f);
                    if (cc != null) cc.enabled = true;
                }
            }

            // 6. Clean up knowledge store
            InvestigationKnowledgeService.Instance?.Clear();

            // 7. Reset simulation clock to 00:00
            SimulationManager.Instance?.ResetSimulation();

            if (playFanfare)
            {
                AudioManager.Instance?.PlayEvidenceDiscovery();
                UIManager.Instance?.ShowToast(
                    "NEW CASE INITIATED",
                    $"Meridian House generated for Seed: {seed.SeedCode}. Case: '{_currentInvestigation.GroundTruth.CaseTitle}'.",
                    TomorrowsCrime.UI.Styling.UITheme.BrassHighlight);
            }
        }

        private Dictionary<string, Material> ResolveMaterials()
        {
            var dict = new Dictionary<string, Material>();
#if UNITY_EDITOR
            var type = System.Type.GetType("TomorrowsCrime.EditorTools.ArtAssetSetup, TomorrowsCrime.Editor");
            if (type != null)
            {
                var method = type.GetMethod("CreateOrUpdateMaterials", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    return method.Invoke(null, null) as Dictionary<string, Material> ?? dict;
                }
            }
#endif
            return dict;
        }

        public void OnLoopReset()
        {
            // Reset world props and dynamic states
            Debug.Log($"[GameSessionManager] Resetting case states for Loop turnover.");
        }
    }
}
