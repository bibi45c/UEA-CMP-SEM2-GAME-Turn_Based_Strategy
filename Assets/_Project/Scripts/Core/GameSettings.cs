using UnityEngine;

namespace TurnBasedTactics.Core
{
    /// <summary>
    /// Lightweight persistent settings. Values survive scene reloads via static fields.
    /// QualityLevel maps: 0=Low, 2=Medium, 5=High.
    /// </summary>
    public static class GameSettings
    {
        public static float BGMVolume    { get; set; } = 0.7f;
        public static float SFXVolume    { get; set; } = 0.8f;
        public static int   QualityLevel { get; set; } = 2;

        public static void ApplyAll()
        {
            QualitySettings.SetQualityLevel(QualityLevel, applyExpensiveChanges: true);
        }
    }
}
