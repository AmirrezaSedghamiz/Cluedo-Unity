using System;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Investigation
{
    /// <summary>
    /// Attach to any world object that yields evidence when inspected by the player.
    /// Integrates with the existing IInteractable / PlayerInteraction raycast system.
    /// </summary>
    public class InspectableEvidence : MonoBehaviour, IInteractable
    {
        [Header("Evidence Configuration")]
        [SerializeField] private EvidenceDefinition evidenceDefinition;
        [SerializeField] private string customPrompt = "Inspect";

        [Header("Discovery Prerequisites")]
        [Tooltip("If true, player must be holding a specific prop in order to discover this evidence.")]
        [SerializeField] private bool requiresHoldingProp;
        [SerializeField] private string requiredPropId;

        public event Action<EvidenceDefinition> OnEvidenceInspected;

        public EvidenceDefinition Definition
        {
            get => evidenceDefinition;
            set => evidenceDefinition = value;
        }

        public bool RequiresHoldingProp
        {
            get => requiresHoldingProp;
            set => requiresHoldingProp = value;
        }

        public string RequiredPropId
        {
            get => requiredPropId;
            set => requiredPropId = value;
        }

        public string InteractionPrompt
        {
            get
            {
                string name = evidenceDefinition != null ? evidenceDefinition.DisplayName : "Object";
                return $"{customPrompt} {name} [E]";
            }
        }

        public bool CanInteract => CanInspect(out _);

        public bool CanInspect(out string failureReason)
        {
            if (evidenceDefinition == null)
            {
                failureReason = "No evidence definition assigned.";
                return false;
            }

            if (requiresHoldingProp && !string.IsNullOrEmpty(requiredPropId))
            {
                var registry = WorldStateManager.Instance != null ? WorldStateManager.Instance.Registry : null;
                if (registry != null && registry.TryGetState<PropLocationState>(requiredPropId, out var state))
                {
                    if (state != PropLocationState.HeldByPlayer)
                    {
                        failureReason = $"Requires holding item '{requiredPropId}'.";
                        return false;
                    }
                }
                else
                {
                    failureReason = $"Required item '{requiredPropId}' not registered in world state.";
                    return false;
                }
            }

            failureReason = string.Empty;
            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInspect(out string failure))
            {
                Debug.LogWarning($"[InspectableEvidence] Cannot inspect '{name}': {failure}");
                return;
            }

            int loop = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                ? SimulationManager.Instance.Clock.LoopCount
                : 1;

            float simTime = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                ? SimulationManager.Instance.Clock.ElapsedSeconds
                : 0f;

            bool isNew = InvestigationKnowledgeService.Instance.DiscoverEvidence(evidenceDefinition, loop, simTime);

            if (isNew)
            {
                Debug.Log($"[InspectableEvidence] NEW EVIDENCE DISCOVERED: '{evidenceDefinition.DisplayName}' [{evidenceDefinition.EvidenceId}] at {simTime:F1}s (Loop {loop}).");
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowEvidenceModal(evidenceDefinition);
            }

            OnEvidenceInspected?.Invoke(evidenceDefinition);
        }
    }
}
