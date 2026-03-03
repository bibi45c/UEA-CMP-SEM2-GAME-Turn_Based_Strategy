using UnityEngine;

namespace TurnBasedTactics.Core
{
    /// <summary>
    /// ScriptableObject that defines an ordered list of encounter scene names.
    /// Create via Assets → Create → TurnBasedTactics → EncounterList.
    /// </summary>
    [CreateAssetMenu(menuName = "TurnBasedTactics/EncounterList")]
    public class EncounterList : ScriptableObject
    {
        [SerializeField] private string[] _sceneNames;

        public int Count => _sceneNames?.Length ?? 0;

        public string GetSceneName(int index)
        {
            if (_sceneNames == null || index < 0 || index >= _sceneNames.Length)
                return null;
            return _sceneNames[index];
        }
    }

    /// <summary>
    /// Static helper that tracks the current encounter index across scene loads.
    /// No MonoBehaviour — survives scene transitions by being plain static state.
    /// </summary>
    public static class EncounterTracker
    {
        /// <summary>Index into the active EncounterList.</summary>
        public static int CurrentEncounterIndex { get; set; }

        /// <summary>The encounter list being played through. Set at campaign start.</summary>
        public static EncounterList ActiveList { get; set; }

        /// <summary>True if there is another encounter after the current one.</summary>
        public static bool HasNextEncounter()
        {
            return ActiveList != null && CurrentEncounterIndex + 1 < ActiveList.Count;
        }

        /// <summary>Get the scene name of the next encounter (or null).</summary>
        public static string GetNextSceneName()
        {
            if (!HasNextEncounter()) return null;
            return ActiveList.GetSceneName(CurrentEncounterIndex + 1);
        }

        /// <summary>Move to the next encounter index.</summary>
        public static void AdvanceEncounter()
        {
            CurrentEncounterIndex++;
        }

        /// <summary>Reset tracker for a new campaign run.</summary>
        public static void Reset()
        {
            CurrentEncounterIndex = 0;
            ActiveList = null;
        }
    }
}
