using UnityEngine;

namespace TomorrowsCrime.Core
{
    /// <summary>
    /// Contract implemented by any object in the world that the player can interact with
    /// via the first-person raycast interaction system (e.g. keys, doors, switches, clues).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Contextual prompt displayed on the player's HUD (e.g. "Pick up Keycard", "Open Door").
        /// </summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Whether this interactable is currently eligible for player interaction.
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// Executes the interaction logic when triggered by the player.
        /// </summary>
        /// <param name="interactor">The GameObject initiating the interaction (usually the Player).</param>
        void Interact(GameObject interactor);
    }
}
