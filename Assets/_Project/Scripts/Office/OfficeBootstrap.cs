using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TurnBasedTactics.Core;
using TurnBasedTactics.UI;
using TurnBasedTactics.Camera;

namespace TurnBasedTactics.Office
{
    /// <summary>
    /// Bootstraps Office_01 scene.
    /// Attach to an empty "OfficeRoot" GameObject.
    /// Assign PlayerPrefab and PlayerSpawnPoint in Inspector.
    ///
    /// Flow:
    ///   Normal load     → opening cutscene plays → player spawns, can explore
    ///   Sleep trigger   → TriggerSleepSequence() → sleep cutscene → combat scene
    ///   Victory ending  → OfficeBootstrap.ReturnAsVictoryEnding = true before load
    ///                     → 7-frame victory cutscene plays → quit / exit play mode
    /// </summary>
    public class OfficeBootstrap : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private GameObject              _playerPrefab;
        [SerializeField] private Transform               _playerSpawnPoint;
        [SerializeField] private RuntimeAnimatorController _playerAnimController;

        [Header("Combat scene to load after sleep")]
        [SerializeField] private string _combatScene = "Combat_RuinsPrototype_01";

        [Header("Opening cutscene (plays on scene load, before player control)")]
        [SerializeField] private CutsceneSlide[] _openingSlides;

        [Header("Sleep cutscene (plays when interacting with desk)")]
        [SerializeField] private CutsceneSlide[] _sleepSlides;

        [Header("Dungeon cutscene (plays after sleep, while combat scene preloads)")]
        [SerializeField] private CutsceneSlide[] _dungeonSlides;

        [Header("Victory cutscene (plays after final combat victory)")]
        [SerializeField] private CutsceneSlide[] _victorySlides;
        [SerializeField] private Font            _creditsFont;

        /// <summary>Set to true from combat scene before loading office for the victory ending.</summary>
        public static bool ReturnAsVictoryEnding = false;

        // ── Intro cutscene lines (replace with VideoClip later) ───────────
        private static readonly string[] IntroLines =
        {
            "凌晨 3:07\n某游戏公司  开发部",
            "你的游戏刚刚上线……\n1,243 条一星差评。\n全是 bug。",
            "老板今天当着全组的面发飙：\n\n\"明早九点前没有修复版本，\n整个团队解散。\"",
            "同事们都走了。\n只有你还坐在工位前，\n对着满屏报错发呆。",
            "能量饮料凉透了。\n眼皮越来越重……\n\n键盘上，你睡了过去。",
            "屏幕突然亮起一行字：\n\n\nWARNING: Developer has entered\nthe runtime environment."
        };

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            GameSettings.ApplyAll();
            EnsureEventSystem();
            SpawnPlayer();
            SetupCamera();
            SetupOverlayHUD();
            SetupCutsceneController();
        }

        private void Start()
        {
            if (ReturnAsVictoryEnding)
            {
                ReturnAsVictoryEnding = false;
                StartCoroutine(PlayVictoryCutscene());
                return;
            }
            StartCoroutine(PlayOpeningCutscene());
        }

        // Plays the 5-frame opening slideshow, then hands control to the player.
        private IEnumerator PlayOpeningCutscene()
        {
            OfficePlayerController.Instance?.DisableInput();

            if (_openingSlides != null && _openingSlides.Length > 0)
            {
                bool done = false;
                // Wait one frame so CutsceneController.Awake has run.
                yield return null;
                CutsceneController.Instance?.PlaySlides(_openingSlides, () => done = true);
                yield return new WaitUntil(() => done);
            }
            else
            {
                // Fallback: brief time overlay if no slides are wired
                yield return StartCoroutine(PlayTimeOverlay());
                yield break;
            }

            OfficePlayerController.Instance?.EnableInput();
        }

        // Shows "凌晨 3:00" for 3 seconds then hands control to the player.
        private IEnumerator PlayTimeOverlay()
        {
            OfficePlayerController.Instance?.DisableInput();

            // Build a simple fullscreen text overlay
            var canvas = new GameObject("TimeOverlay").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var cs = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);

            // Dark background
            var bg = new GameObject("Bg").AddComponent<UnityEngine.UI.Image>();
            bg.transform.SetParent(canvas.transform, false);
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

            // Time text
            var textGO = new GameObject("TimeText").AddComponent<UnityEngine.UI.Text>();
            textGO.transform.SetParent(canvas.transform, false);
            textGO.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textGO.fontSize  = 52;
            textGO.fontStyle = FontStyle.Bold;
            textGO.alignment = TextAnchor.MiddleCenter;
            textGO.color     = Color.white;
            textGO.text      = "凌晨  3:00\n某游戏公司  开发部";
            var tRect = textGO.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.3f, 0.4f); tRect.anchorMax = new Vector2(0.7f, 0.6f);
            tRect.offsetMin = tRect.offsetMax = Vector2.zero;

            // Hold 2.5s then fade out over 0.8s
            yield return new WaitForSeconds(2.5f);

            float t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                float a = 1f - t / 0.8f;
                textGO.color = new Color(1, 1, 1, a);
                bg.color     = new Color(0, 0, 0, 0.6f * a);
                yield return null;
            }

            Destroy(canvas.gameObject);
            OfficePlayerController.Instance?.EnableInput();
        }

        // ── Public: called by the desk's OfficeInteractable.OnInteract ────

        public void TriggerSleepSequence()
        {
            OfficePlayerController.Instance?.DisableInput();
            StartCoroutine(SleepThenPlay());
        }

        // ── Private helpers ───────────────────────────────────────────────

        private IEnumerator SleepThenPlay()
        {
            yield return new WaitForSeconds(0.4f);

            // ── 1. Sleep cutscene (frame2-A~H) — keep black after so the dungeon
            //     cutscene can take over without flashing the office in between. ──
            bool sleepDone = false;
            if (_sleepSlides != null && _sleepSlides.Length > 0)
                CutsceneController.Instance?.PlaySlides(
                    _sleepSlides, () => sleepDone = true, keepBlackAfter: true);
            else
                CutsceneController.Instance?.Play(IntroLines, () => sleepDone = true);
            yield return new WaitUntil(() => sleepDone);

            // ── 2. Kick off async load of combat scene WITHOUT activating ──
            //     Scene loads in background but its Awake/Start (including BGM)
            //     stays deferred until allowSceneActivation = true.
            AsyncOperation loadOp = null;
            if (!string.IsNullOrEmpty(_combatScene))
            {
                loadOp = SceneManager.LoadSceneAsync(_combatScene);
                loadOp.allowSceneActivation = false;
            }

            // ── 3. Dungeon cutscene — keep canvas opaque after last frame so we
            //     don't flash the office scene while the combat scene activates. ──
            if (_dungeonSlides != null && _dungeonSlides.Length > 0)
            {
                bool dungeonDone = false;
                CutsceneController.Instance?.PlaySlides(
                    _dungeonSlides, () => dungeonDone = true, keepBlackAfter: true);
                yield return new WaitUntil(() => dungeonDone);
            }

            // ── 4. Wait for preload to finish, then activate. The cutscene canvas
            //     is unloaded together with this scene, the new scene takes over. ──
            if (loadOp != null)
            {
                while (loadOp.progress < 0.9f) yield return null;
                loadOp.allowSceneActivation = true;
            }
        }

        // Plays the 7-frame victory cutscene after the player wins the final combat.
        // Hides the office player so we don't see the protagonist spawned in the room,
        // then quits to editor / desktop once the cutscene completes.
        private IEnumerator PlayVictoryCutscene()
        {
            OfficePlayerController.Instance?.DisableInput();
            var player = GameObject.Find("OfficePlayer");
            if (player != null) player.SetActive(false);

            yield return null; // let CutsceneController.Awake finish

            if (_victorySlides != null && _victorySlides.Length > 0)
            {
                bool done = false;
                CutsceneController.Instance?.PlaySlides(
                    _victorySlides, () => done = true, keepBlackAfter: true);
                yield return new WaitUntil(() => done);
            }
            else
            {
                Debug.LogWarning("[OfficeBootstrap] _victorySlides is empty; ending immediately.");
            }

            Debug.Log("[OfficeBootstrap] Victory ending complete.");
            yield return StartCoroutine(ShowCreditsAndQuit());
        }

        // Final credits — "Made by Jiayu Jiang" in PirataOne, click to quit.
        private IEnumerator ShowCreditsAndQuit()
        {
            // Build canvas above the cutscene canvas
            var canvasGO = new GameObject("CreditsCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;

            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Black backdrop (re-asserts black on top of whatever the cutscene left)
            var bgGO = new GameObject("Bg");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bg = bgGO.AddComponent<UnityEngine.UI.Image>();
            bg.color = Color.black;
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

            // Use the assigned credits font, fallback to built-in
            var creditsFont = _creditsFont != null
                ? _creditsFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Credit title — centered
            var titleGO = new GameObject("Credit");
            titleGO.transform.SetParent(canvasGO.transform, false);
            var title = titleGO.AddComponent<UnityEngine.UI.Text>();
            title.font = creditsFont;
            title.fontSize = 96;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.85f, 0.68f, 0.18f, 0f); // gold, start transparent for fade-in
            title.text = "Made by Jiayu Jiang";
            var titleOutline = titleGO.AddComponent<UnityEngine.UI.Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            titleOutline.effectDistance = new Vector2(2f, -2f);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 0.5f);
            titleRT.anchorMax = new Vector2(0.5f, 0.5f);
            titleRT.pivot = new Vector2(0.5f, 0.5f);
            titleRT.anchoredPosition = new Vector2(0f, 40f);
            titleRT.sizeDelta = new Vector2(1400f, 160f);

            // Subtitle — AI collaboration credit
            var subGO = new GameObject("CreditSub");
            subGO.transform.SetParent(canvasGO.transform, false);
            var sub = subGO.AddComponent<UnityEngine.UI.Text>();
            sub.font = creditsFont;
            sub.fontSize = 36;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.color = new Color(0.70f, 0.58f, 0.20f, 0f); // dimmer gold
            sub.text = "In collaboration with Opus 4.7 & GPT 5.5";
            var subOutline = subGO.AddComponent<UnityEngine.UI.Outline>();
            subOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            subOutline.effectDistance = new Vector2(1f, -1f);
            var subRT = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.5f, 0.5f);
            subRT.anchorMax = new Vector2(0.5f, 0.5f);
            subRT.pivot = new Vector2(0.5f, 0.5f);
            subRT.anchoredPosition = new Vector2(0f, -60f);
            subRT.sizeDelta = new Vector2(1400f, 60f);

            // Bottom-right hint
            var hintGO = new GameObject("Hint");
            hintGO.transform.SetParent(canvasGO.transform, false);
            var hint = hintGO.AddComponent<UnityEngine.UI.Text>();
            hint.font = creditsFont;
            hint.fontSize = 24;
            hint.alignment = TextAnchor.MiddleRight;
            hint.color = new Color(0.7f, 0.7f, 0.7f, 0f);
            hint.text = "Click to close  ✕";
            var hintRT = hintGO.GetComponent<RectTransform>();
            hintRT.anchorMin = new Vector2(1f, 0f);
            hintRT.anchorMax = new Vector2(1f, 0f);
            hintRT.pivot = new Vector2(1f, 0f);
            hintRT.anchoredPosition = new Vector2(-30f, 28f);
            hintRT.sizeDelta = new Vector2(260f, 36f);

            // Fade in title (1.0s)
            float t = 0f;
            const float titleFade = 1.0f;
            while (t < titleFade)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / titleFade);
                title.color = new Color(0.85f, 0.68f, 0.18f, a);
                yield return null;
            }
            title.color = new Color(0.85f, 0.68f, 0.18f, 1f);

            // Brief pause, then fade in subtitle (0.7s)
            yield return new WaitForSecondsRealtime(0.4f);
            t = 0f;
            const float subFade = 0.7f;
            while (t < subFade)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / subFade);
                sub.color = new Color(0.70f, 0.58f, 0.20f, a);
                yield return null;
            }
            sub.color = new Color(0.70f, 0.58f, 0.20f, 1f);

            // Hold a moment, then fade in hint
            yield return new WaitForSecondsRealtime(0.6f);
            t = 0f;
            const float hintFade = 0.5f;
            while (t < hintFade)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / hintFade);
                hint.color = new Color(0.7f, 0.7f, 0.7f, a);
                yield return null;
            }
            hint.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            // Wait for left click
            yield return new WaitUntil(() =>
                UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SpawnPlayer()
        {
            if (_playerPrefab == null || _playerSpawnPoint == null)
            {
                Debug.LogWarning("[OfficeBootstrap] PlayerPrefab or SpawnPoint not assigned.");
                return;
            }
            var player = Instantiate(_playerPrefab,
                _playerSpawnPoint.position, _playerSpawnPoint.rotation);
            player.name = "OfficePlayer";

            // Shift CC center up so capsule bottom = transform.y (feet on floor)
            // With center=(0,1,0) height=2 radius=0.5: bottom = transform.y + 1 - 1 = transform.y
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.center = new Vector3(0f, 1f, 0f);

            // Assign Synty locomotion controller
            if (_playerAnimController != null)
            {
                var anim = player.GetComponentInChildren<Animator>();
                if (anim != null) anim.runtimeAnimatorController = _playerAnimController;
            }

            if (player.GetComponent<OfficePlayerController>() == null)
                player.AddComponent<OfficePlayerController>();
        }

        private void SetupCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            var tactical = cam.GetComponent<TacticalCamera>();
            if (tactical == null)
                tactical = cam.gameObject.AddComponent<TacticalCamera>();

            var player = GameObject.Find("OfficePlayer");
            if (player != null)
            {
                tactical.SetFollowTarget(player.transform);
                tactical.SetZoom(8f, instant: true);
                player.GetComponent<OfficePlayerController>()?.SetCamera(cam.transform);
            }
        }

        private void SetupOverlayHUD()
        {
            var go = new GameObject("GameOverlayHUD");
            go.transform.SetParent(transform, false);
            go.AddComponent<GameOverlayHUD>().Initialize();
        }

        private void SetupCutsceneController()
        {
            var go = new GameObject("CutsceneController");
            go.transform.SetParent(transform, false);
            go.AddComponent<CutsceneController>();
        }

        private static void EnsureEventSystem()
        {
            var existing = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existing != null)
            {
                // Replace legacy StandaloneInputModule with new Input System module
                var legacy = existing.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (legacy != null)
                {
                    Object.Destroy(legacy);
                    if (existing.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                        existing.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
                return;
            }
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
