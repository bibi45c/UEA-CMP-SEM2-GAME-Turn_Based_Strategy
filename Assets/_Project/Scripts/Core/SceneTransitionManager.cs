using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TurnBasedTactics.Core
{
    /// <summary>
    /// DontDestroyOnLoad singleton that handles scene transitions with
    /// a fade-to-black / fade-from-black overlay. Blocks all input during fade.
    /// Created on first access via Instance property.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        private static SceneTransitionManager _instance;

        /// <summary>Lazy singleton — creates itself if needed.</summary>
        public static SceneTransitionManager Instance
        {
            get
            {
                if (_instance == null)
                    EnsureInstance();
                return _instance;
            }
        }

        private Image _fadeImage;
        private Canvas _fadeCanvas;

        [Header("Transition Settings")]
        private float _fadeOutDuration = 0.5f;
        private float _fadeInDuration = 0.5f;

        /// <summary>True while a scene transition is in progress.</summary>
        public bool IsTransitioning { get; private set; }

        // --- Public API ---

        /// <summary>Fade out, load scene by build index, fade in.</summary>
        public void TransitionToScene(int buildIndex)
        {
            if (IsTransitioning) return;
            StartCoroutine(TransitionCoroutine(() => SceneManager.LoadSceneAsync(buildIndex)));
        }

        /// <summary>Fade out, load scene by name, fade in.</summary>
        public void TransitionToScene(string sceneName)
        {
            if (IsTransitioning) return;
            StartCoroutine(TransitionCoroutine(() => SceneManager.LoadSceneAsync(sceneName)));
        }

        /// <summary>Fade out, reload the current scene, fade in.</summary>
        public void RestartCurrentScene()
        {
            if (IsTransitioning) return;
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            string currentName = SceneManager.GetActiveScene().name;

            // Use name if build index is invalid (-1 when not in build settings)
            if (currentIndex >= 0)
                StartCoroutine(TransitionCoroutine(() => SceneManager.LoadSceneAsync(currentIndex)));
            else
                StartCoroutine(TransitionCoroutine(() => SceneManager.LoadSceneAsync(currentName)));
        }

        // --- Transition Coroutine ---

        private IEnumerator TransitionCoroutine(System.Func<AsyncOperation> loadFunc)
        {
            IsTransitioning = true;
            _fadeImage.raycastTarget = true; // Block all input during transition

            // Fade out (transparent → black)
            yield return FadeCoroutine(0f, 1f, _fadeOutDuration);

            // Load scene
            var loadOp = loadFunc();
            if (loadOp != null)
            {
                while (!loadOp.isDone)
                    yield return null;
            }

            // Brief pause for systems to initialize
            yield return null;
            yield return null;

            // Fade in (black → transparent)
            yield return FadeCoroutine(1f, 0f, _fadeInDuration);

            _fadeImage.raycastTarget = false; // Restore input
            IsTransitioning = false;

            Debug.Log("[SceneTransitionManager] Transition complete.");
        }

        private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                _fadeImage.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            _fadeImage.color = new Color(0f, 0f, 0f, toAlpha);
        }

        // --- Singleton Bootstrap ---

        private static void EnsureInstance()
        {
            if (_instance != null) return;

            var go = new GameObject("[SceneTransitionManager]");
            _instance = go.AddComponent<SceneTransitionManager>();
            DontDestroyOnLoad(go);
            _instance.CreateFadeCanvas();

            Debug.Log("[SceneTransitionManager] Created.");
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_fadeCanvas == null)
                CreateFadeCanvas();
        }

        private void CreateFadeCanvas()
        {
            // Canvas
            var canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform, false);

            _fadeCanvas = canvasGO.AddComponent<Canvas>();
            _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _fadeCanvas.sortingOrder = 999;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Full-screen black image
            var imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(canvasGO.transform, false);

            _fadeImage = imageGO.AddComponent<Image>();
            _fadeImage.color = new Color(0f, 0f, 0f, 0f); // Start transparent
            _fadeImage.raycastTarget = false;

            // Stretch to fill
            var rt = imageGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
