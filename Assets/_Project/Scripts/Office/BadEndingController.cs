using UnityEngine;
using UnityEngine.SceneManagement;
using TurnBasedTactics.Core;
using TurnBasedTactics.Combat;

namespace TurnBasedTactics.Office
{
    /// <summary>
    /// Add to CombatRoot (or any persistent GameObject in the combat scene).
    /// Subscribes to CombatEndedEvent. If the player lost, triggers the bad ending:
    /// sets OfficeBootstrap.ReturnAsBadEnding = true and loads the office scene.
    ///
    /// Normal victory uses the existing CombatResultsScreen flow unchanged.
    /// </summary>
    public class BadEndingController : MonoBehaviour
    {
        [SerializeField] private string _officeScene = "Office_01";

        private void OnEnable()
        {
            EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        }

        private void OnCombatEnded(CombatEndedEvent evt)
        {
            // WinningTeamId == 0 → players won; anything else → players lost
            if (evt.WinningTeamId == 0) return;

            Debug.Log("[BadEnding] Player lost — loading office bad ending.");
            OfficeBootstrap.ReturnAsBadEnding = true;
            SceneManager.LoadScene(_officeScene);
        }
    }
}
