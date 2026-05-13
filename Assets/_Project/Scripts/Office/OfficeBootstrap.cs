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
    ///   Normal load  → opening cutscene plays → player spawns, can explore
    ///   Sleep trigger → TriggerSleepSequence() → sleep cutscene → combat scene
    ///   Bad ending   → OfficeBootstrap.ReturnAsBadEnding = true before load
    ///                  → bad ending cutscene plays, player hidden
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

        /// <summary>Set to true from combat scene before loading office for bad ending.</summary>
        public static bool ReturnAsBadEnding = false;

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

        // ── Bad ending lines ──────────────────────────────────────────────
        private static readonly string[] BadEndingLines =
        {
            "你没能回来。",
            "同事们以为你只是请假了。\n工位上的咖啡杯还没人收走。",
            "屏幕上最后一条 commit 记录：\n\nfix: developer.exe  —  FAILED",
            "\n[ GAME OVER ]"
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
            if (ReturnAsBadEnding)
            {
                ReturnAsBadEnding = false;
                StartCoroutine(ShowBadEnding());
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

        private IEnumerator ShowBadEnding()
        {
            OfficePlayerController.Instance?.DisableInput();
            var player = GameObject.Find("OfficePlayer");
            if (player != null) player.SetActive(false);

            yield return new WaitForSeconds(0.5f);
            CutsceneController.Instance?.Play(BadEndingLines, () =>
            {
                // Stay on the empty office — player can close the game or restart
                Debug.Log("[OfficeBootstrap] Bad ending complete.");
            });
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
