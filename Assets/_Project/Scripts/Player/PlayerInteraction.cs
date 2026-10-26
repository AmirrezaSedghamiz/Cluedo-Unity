using UnityEngine;
using TomorrowsCrime.Core;

namespace TomorrowsCrime.Player
{
    /// <summary>
    /// First-person raycast interaction handler.
    /// Detects IInteractable components in front of the player camera and triggers
    /// interactions when the player presses the interaction key (E).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [Tooltip("Maximum distance in meters at which the player can interact with objects.")]
        [SerializeField] private float interactionRange = 3.0f;

        [Tooltip("Layer mask filter for interactable geometry.")]
        [SerializeField] private LayerMask interactableLayers = ~0;

        [Header("References")]
        [SerializeField] private Camera playerCamera;

        public IInteractable CurrentInteractable { get; private set; }
        public string CurrentPrompt { get; private set; }
        public bool HasInteractable => CurrentInteractable != null && CurrentInteractable.CanInteract;

        private PlayerController _playerController;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            if ((_playerController != null && _playerController.IsInputBlocked) ||
                (TomorrowsCrime.UI.Core.UIManager.Instance != null && TomorrowsCrime.UI.Core.UIManager.Instance.IsModalOpen))
            {
                CurrentInteractable = null;
                CurrentPrompt = null;
                return;
            }

            DetectInteractable();
            HandleInteractionInput();
        }

        private void DetectInteractable()
        {
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayers))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract)
                {
                    CurrentInteractable = interactable;
                    CurrentPrompt = interactable.InteractionPrompt;
                    return;
                }
            }

            CurrentInteractable = null;
            CurrentPrompt = null;
        }

        private void HandleInteractionInput()
        {
            if (Input.GetKeyDown(KeyCode.E) && HasInteractable)
            {
                try
                {
                    CurrentInteractable.Interact(gameObject);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PlayerInteraction] Error during interaction with {CurrentInteractable}: {ex}");
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerCamera != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionRange);
            }
        }
    }
}
