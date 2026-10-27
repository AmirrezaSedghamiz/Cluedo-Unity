using UnityEngine;
using TomorrowsCrime.Core;

namespace TomorrowsCrime.World
{
    /// <summary>
    /// Interactive button for calling the elevator or selecting a destination floor.
    /// Implements IInteractable for direct first-person raycast interaction.
    /// </summary>
    public class InteractableElevatorButton : MonoBehaviour, IInteractable
    {
        [Header("Elevator Link")]
        [SerializeField] private HotelElevator elevator;
        [SerializeField] private int targetFloor = 0;
        [SerializeField] private string floorDisplayName = "Grand Marble Lobby";
        [SerializeField] private bool isCallButton = false;

        public HotelElevator Elevator
        {
            get => elevator;
            set => elevator = value;
        }

        public int TargetFloor
        {
            get => targetFloor;
            set => targetFloor = value;
        }

        public string FloorDisplayName
        {
            get => floorDisplayName;
            set => floorDisplayName = value;
        }

        public bool IsCallButton
        {
            get => isCallButton;
            set => isCallButton = value;
        }

        public string InteractionPrompt
        {
            get
            {
                if (elevator == null) return "Elevator Button";
                if (elevator.IsMoving) return "Elevator In Motion...";

                return isCallButton
                    ? $"Call Elevator to Floor {targetFloor} [E]"
                    : $"Take Elevator to Floor {targetFloor}: {floorDisplayName} [E]";
            }
        }

        public bool CanInteract => elevator != null && !elevator.IsMoving;

        public void Interact(GameObject interactor)
        {
            if (elevator == null || elevator.IsMoving) return;

            elevator.MoveToFloor(targetFloor);
        }
    }
}
