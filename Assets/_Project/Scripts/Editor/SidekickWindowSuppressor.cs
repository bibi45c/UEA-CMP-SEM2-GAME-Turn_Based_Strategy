using UnityEditor;
using UnityEngine;

namespace TurnBasedTactics.Editor
{
    /// <summary>
    /// Closes the Synty Sidekick Tool Downloader popup on every editor startup.
    /// We don't need the Sidekick tool — this just prevents the window from auto-restoring.
    /// </summary>
    [InitializeOnLoad]
    public static class SidekickWindowSuppressor
    {
        static SidekickWindowSuppressor()
        {
            EditorApplication.update += CloseOnFirstUpdate;
        }

        private static void CloseOnFirstUpdate()
        {
            EditorApplication.update -= CloseOnFirstUpdate;

            foreach (var win in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                try
                {
                    if (win != null && win.titleContent != null &&
                        win.titleContent.text == "Sidekick Tool Downloader")
                    {
                        win.Close();
                        break;
                    }
                }
                catch { /* window already destroyed */ }
            }
        }
    }
}
