using UnityEngine;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// Thin MonoBehaviour binding on each unit's root GameObject.
    /// Bridges Unity scene (GO) to domain logic (UnitRuntime).
    /// No Update(), no heavy logic — just holds the reference.
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitBrain : MonoBehaviour
    {
        private UnitRuntime _runtime;

        public UnitRuntime Runtime => _runtime;
        public int UnitId => _runtime?.UnitId ?? -1;
        public bool IsInitialized => _runtime != null;

        /// <summary>
        /// Called by UnitSpawner after creation. Not serialized.
        /// </summary>
        public void Initialize(UnitRuntime runtime)
        {
            _runtime = runtime;
        }
    }
}
