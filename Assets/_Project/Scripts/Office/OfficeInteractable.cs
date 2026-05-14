using UnityEngine;
using UnityEngine.Events;

namespace TurnBasedTactics.Office
{
    /// <summary>
    /// Add to any GameObject to make it interactable (press E when player is in range).
    /// OfficePlayerController scans for the nearest active Interactable each frame.
    /// Wire OnInteract in the Inspector or call AddListener in code.
    /// </summary>
    public class OfficeInteractable : MonoBehaviour
    {
        [Tooltip("Label shown on the E-key prompt, e.g. '查看' or '睡着'")]
        public string Label = "查看";

        public UnityEvent OnInteract;

        public bool IsInRange { get; private set; }

        public void SetInRange(bool value) => IsInRange = value;

        public void Interact()
        {
            if (!IsInRange) return;
            OnInteract?.Invoke();
        }
    }
}
