using System;
using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Player;
using TomorrowsCrime.Simulation;

namespace TomorrowsCrime.UI.Core
{
    [Serializable]
    public class NotificationToast
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public float TimeRemaining { get; set; }
        public float TotalDuration { get; set; }
        public Color AccentColor { get; set; }
    }

    /// <summary>
    /// Central manager for UI presentation, screen state transitions, cursor arbitration,
    /// and player input coordination.
    /// Ensures modal views do not leak inputs into first-person gameplay.
    /// </summary>
    public class UIManager : MonoBehaviour, ILoopResetReceiver
    {
        private static UIManager _instance;
        public static UIManager Instance => _instance;

        [Header("State")]
        [SerializeField] private UIState currentState = UIState.InGameHUD;
        [SerializeField] private bool startInMainMenu = false;

        public UIState CurrentState => currentState;
        public bool IsModalOpen => currentState != UIState.InGameHUD;
        public bool IsTypingInField { get; set; }

        public event Action<UIState, UIState> OnStateChanged;

        private UIState _previousState = UIState.InGameHUD;
        private PlayerController _playerController;
        private EvidenceDefinition _currentInspectedEvidence;
        private TomorrowsCrime.NPC.NPCController _currentInteractingNpc;
        private readonly List<NotificationToast> _activeToasts = new();

        public IReadOnlyList<NotificationToast> ActiveToasts => _activeToasts.AsReadOnly();
        public EvidenceDefinition CurrentInspectedEvidence => _currentInspectedEvidence;
        public TomorrowsCrime.NPC.NPCController CurrentInteractingNpc => _currentInteractingNpc;

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
            FindPlayerController();

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }

            SubscribeToInvestigationEvents();

            if (startInMainMenu)
            {
                SetState(UIState.MainMenu);
            }
            else
            {
                SetState(UIState.InGameHUD);
            }
        }

        private void OnDestroy()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.UnregisterResetReceiver(this);
            }

            UnsubscribeFromInvestigationEvents();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void FindPlayerController()
        {
            if (_playerController == null)
            {
                _playerController = FindAnyObjectByType<PlayerController>();
            }
        }

        private void SubscribeToInvestigationEvents()
        {
            var knowledge = InvestigationKnowledgeService.Instance;
            if (knowledge != null)
            {
                knowledge.OnNewEvidenceDiscoveredNotification += HandleNewEvidenceDiscovered;
                knowledge.OnObservationNotification += HandleObservationNotification;
            }
        }

        private void UnsubscribeFromInvestigationEvents()
        {
            var knowledge = InvestigationKnowledgeService.Instance;
            if (knowledge != null)
            {
                knowledge.OnNewEvidenceDiscoveredNotification -= HandleNewEvidenceDiscovered;
                knowledge.OnObservationNotification -= HandleObservationNotification;
            }
        }

        private void Update()
        {
            UpdateToasts(Time.unscaledDeltaTime);
            HandleGlobalShortcuts();
        }

        private void UpdateToasts(float deltaTime)
        {
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                _activeToasts[i].TimeRemaining -= deltaTime;
                if (_activeToasts[i].TimeRemaining <= 0f)
                {
                    _activeToasts.RemoveAt(i);
                }
            }
        }

        private void HandleGlobalShortcuts()
        {
            if (IsTypingInField) return;

            // Notebook toggle shortcut (Tab or N)
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.N))
            {
                if (currentState == UIState.InGameHUD)
                {
                    SetState(UIState.InvestigationNotebook);
                }
                else if (currentState == UIState.InvestigationNotebook)
                {
                    SetState(UIState.InGameHUD);
                }
            }

            // Escape navigation
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscape();
            }
        }

        public void HandleEscape()
        {
            switch (currentState)
            {
                case UIState.InGameHUD:
                    SetState(UIState.PauseMenu);
                    break;

                case UIState.PauseMenu:
                    SetState(UIState.InGameHUD);
                    break;

                case UIState.SettingsMenu:
                    SetState(_previousState == UIState.MainMenu ? UIState.MainMenu : UIState.PauseMenu);
                    break;

                case UIState.InvestigationNotebook:
                    SetState(UIState.InGameHUD);
                    break;

                case UIState.EvidenceDetailModal:
                    CloseModal();
                    break;

                case UIState.NPCDialogueModal:
                    CloseModal();
                    break;

                case UIState.CaseResolutionModal:
                    CloseModal();
                    break;

                case UIState.LoopSummaryOverlay:
                    SetState(UIState.InGameHUD);
                    break;
            }
        }

        public void SetState(UIState newState)
        {
            if (currentState == newState) return;

            UIState oldState = currentState;
            _previousState = oldState;
            currentState = newState;

            ApplyStateSideEffects(oldState, newState);
            OnStateChanged?.Invoke(oldState, newState);
        }

        private void ApplyStateSideEffects(UIState oldState, UIState newState)
        {
            FindPlayerController();

            // 1. Cursor & Input Management
            if (newState == UIState.InGameHUD)
            {
                if (_playerController != null)
                {
                    _playerController.SetInputBlocked(false);
                }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                if (_playerController != null)
                {
                    _playerController.SetInputBlocked(true);
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (newState == UIState.InvestigationNotebook || newState == UIState.EvidenceDetailModal)
                {
                    TomorrowsCrime.Audio.AudioManager.Instance?.PlayPaperRustle();
                }
            }

            // 2. Simulation Pause Management
            if (newState == UIState.PauseMenu || (newState == UIState.SettingsMenu && _previousState == UIState.PauseMenu))
            {
                if (SimulationManager.Instance != null)
                {
                    SimulationManager.Instance.IsPaused = true;
                }
            }
            else if (oldState == UIState.PauseMenu || (oldState == UIState.SettingsMenu && _previousState == UIState.PauseMenu))
            {
                if (SimulationManager.Instance != null && newState == UIState.InGameHUD)
                {
                    SimulationManager.Instance.IsPaused = false;
                }
            }
        }

        public void ShowEvidenceModal(EvidenceDefinition definition)
        {
            if (definition == null) return;
            _currentInspectedEvidence = definition;
            SetState(UIState.EvidenceDetailModal);
        }

        public void ShowDialogueModal(TomorrowsCrime.NPC.NPCController npc)
        {
            if (npc == null) return;
            _currentInteractingNpc = npc;
            SetState(UIState.NPCDialogueModal);
            TomorrowsCrime.Audio.AudioManager.Instance?.PlayPaperRustle();
        }

        public void ShowCaseResolutionModal(TomorrowsCrime.Investigation.AccusationResult result)
        {
            if (result == null) return;
            var resolutionView = FindAnyObjectByType<TomorrowsCrime.UI.Modals.CaseResolutionModalView>();
            if (resolutionView != null)
            {
                resolutionView.SetResult(result);
            }
            SetState(UIState.CaseResolutionModal);
        }

        public void CloseModal()
        {
            if (_currentInteractingNpc != null)
            {
                _currentInteractingNpc.EndConversation();
                _currentInteractingNpc = null;
            }
            _currentInspectedEvidence = null;
            SetState(_previousState == UIState.InvestigationNotebook ? UIState.InvestigationNotebook : UIState.InGameHUD);
        }

        public void ShowToast(string title, string message, Color accentColor, float duration = 4.5f)
        {
            _activeToasts.Insert(0, new NotificationToast
            {
                Title = title,
                Message = message,
                TimeRemaining = duration,
                TotalDuration = duration,
                AccentColor = accentColor
            });

            if (_activeToasts.Count > 4)
            {
                _activeToasts.RemoveAt(_activeToasts.Count - 1);
            }
        }

        private void HandleNewEvidenceDiscovered(DiscoveredEvidenceRecord record)
        {
            ShowToast(
                "EVIDENCE CATALOGED",
                $"{record.Definition.DisplayName} has been added to your investigation notebook. [Tab]",
                new Color(0.71f, 0.60f, 0.38f, 1.0f) // Muted Brass
            );
        }

        private void HandleObservationNotification(string description)
        {
            ShowToast(
                "EVENT WITNESSED",
                description,
                new Color(0.43f, 0.61f, 0.60f, 1.0f) // Desaturated Teal
            );
        }

        public void TriggerLoopResetFromMenu()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.ResetSimulation();
                SetState(UIState.InGameHUD);
                ShowToast("LOOP RESET", "The 5-minute loop has returned to baseline. Persistent investigation data preserved.", new Color(0.85f, 0.60f, 0.29f, 1.0f));
            }
        }

        public void OnLoopReset()
        {
            // Close any open transient inspection modals on reset
            if (currentState == UIState.EvidenceDetailModal)
            {
                CloseModal();
            }

            ShowToast("LOOP RESTARTED", "Time reverted to 12:00:00. Meridian House returned to baseline conditions.", new Color(0.85f, 0.60f, 0.29f, 1.0f));
        }
    }
}
