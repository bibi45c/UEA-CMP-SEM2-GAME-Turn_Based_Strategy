using UnityEngine;
using UnityEngine.EventSystems;
using TurnBasedTactics.Combat;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Thin compatibility shim. The legacy OnGUI HUD has been replaced by ActionBar (uGUI).
    /// This class only exists to maintain the static IsMouseOverHud property
    /// consumed by TacticalInputHandler, now sourced from EventSystem.
    /// </summary>
    public class CombatHudController : MonoBehaviour
    {
        /// <summary>
        /// Returns true if the mouse pointer is over any uGUI element.
        /// Replaces the old OnGUI rect-based hit test.
        /// </summary>
        public static bool IsMouseOverHud { get; private set; }

        private void Update()
        {
            IsMouseOverHud = EventSystem.current != null
                          && EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// Kept for API compatibility with GameBootstrap.InitializeCombatHud().
        /// No-op since ActionBar handles all HUD rendering now.
        /// </summary>
        public void Initialize(CombatSceneController combatController)
        {
            // ActionBar handles all HUD duties.
            // This component only provides IsMouseOverHud.
        }
    }
}
