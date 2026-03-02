using System;
using UnityEngine;

namespace TurnBasedTactics.Camera
{
    /// <summary>
    /// Configuration data for TacticalCamera.
    /// Serialized inline on the camera MonoBehaviour.
    /// Can be promoted to ScriptableObject later if multiple camera profiles are needed.
    /// </summary>
    [Serializable]
    public class TacticalCameraConfig
    {
        [Header("Zoom")]
        public float MinZoomDistance = 5f;
        public float MaxZoomDistance = 25f;
        public float DefaultZoomDistance = 12f;
        public float ZoomSpeed = 2f;
        public float ZoomDamping = 8f;

        [Header("Rotation")]
        public float RotationSpeed = 90f;
        public float RotationDamping = 10f;

        [Header("Pan")]
        public float KeyboardPanSpeed = 10f;
        public float MouseDragPanSpeed = 0.3f;
        public float PanDamping = 8f;

        [Header("Angle")]
        [Range(20f, 80f)]
        public float Pitch = 50f;

        [Header("Follow")]
        public float FollowDamping = 6f;
        public float PanResetSpeed = 3f;
    }
}
