using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace TurnBasedTactics.Office
{
    [System.Serializable]
    public struct CutsceneSlide
    {
        public Sprite image;
        [TextArea(2, 4)]
        public string caption;
    }

    /// <summary>
    /// Fullscreen cutscene player. Three modes:
    /// 1. Slideshow: PlaySlides(slides, ...) — image + caption, left-click to advance
    /// 2. Text-only: Play(lines, ...) — typewriter text cards, any key to advance
    /// 3. Video: assign VideoClip in Inspector
    /// </summary>
    public class CutsceneController : MonoBehaviour
    {
        public static CutsceneController Instance { get; private set; }

        [Header("Video (leave empty → text/slide mode)")]
        [SerializeField] private VideoClip _videoClip;

        private Canvas      _canvas;
        private Image       _blackBg;
        private Image       _slideImage;
        private Image       _captionBar;
        private Text        _captionText;
        private Text        _bodyText;
        private Text        _promptText;
        private VideoPlayer _videoPlayer;

        private string _nextScene;
        private Action _onComplete;
        private bool   _keepBlackAfter;

        private void Awake()
        {
            Instance = this;
            BuildCanvas();
            _canvas.enabled = false;
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Image + caption slideshow, left-click to advance.</summary>
        public void PlaySlides(CutsceneSlide[] slides, string nextScene)
        {
            _nextScene      = nextScene;
            _onComplete     = null;
            _keepBlackAfter = false;
            StartCoroutine(SlideshowRoutine(slides));
        }

        public void PlaySlides(CutsceneSlide[] slides, Action onComplete)
            => PlaySlides(slides, onComplete, keepBlackAfter: false);

        /// <summary>
        /// When <paramref name="keepBlackAfter"/> is true, the canvas stays fully
        /// opaque after the last slide. Use this when the caller will activate a
        /// new scene right after — prevents flashing the underlying old scene.
        /// Caller must invoke FadeOutCanvas() or rely on scene unload to clean up.
        /// </summary>
        public void PlaySlides(CutsceneSlide[] slides, Action onComplete, bool keepBlackAfter)
        {
            _nextScene      = null;
            _onComplete     = onComplete;
            _keepBlackAfter = keepBlackAfter;
            StartCoroutine(SlideshowRoutine(slides));
        }

        /// <summary>Manually fade out the cutscene canvas (used after keepBlackAfter).</summary>
        public void FadeOutCanvas() => StartCoroutine(FadeCanvasOut());

        /// <summary>Text-only mode (typewriter cards).</summary>
        public void Play(string[] lines, string nextScene)
        {
            _nextScene  = nextScene;
            _onComplete = null;
            StartCoroutine(PlayRoutine(lines));
        }

        public void Play(string[] lines, Action onComplete)
        {
            _nextScene  = null;
            _onComplete = onComplete;
            StartCoroutine(PlayRoutine(lines));
        }

        // ── Slideshow ─────────────────────────────────────────────────────

        private IEnumerator SlideshowRoutine(CutsceneSlide[] slides)
        {
            _canvas.enabled    = true;
            ResetCanvasAlpha();
            _blackBg.color     = Color.black;
            _slideImage.enabled  = true;
            _captionBar.enabled  = true;
            _captionText.enabled = true;
            _bodyText.enabled    = false;
            _promptText.enabled  = true;

            foreach (var slide in slides)
            {
                bool hasImage = slide.image != null;

                if (hasImage)
                {
                    _slideImage.enabled         = true;
                    _slideImage.sprite          = slide.image;
                    _slideImage.preserveAspect  = true;
                    _slideImage.color           = new Color(1f, 1f, 1f, 0f);
                }
                else
                {
                    // No image — hide the image entirely so previous frame doesn't bleed through.
                    _slideImage.enabled = false;
                    _slideImage.sprite  = null;
                }

                _captionText.text = slide.caption;
                _promptText.text  = "";

                if (hasImage)
                    yield return StartCoroutine(FadeImage(_slideImage, 0f, 1f, 0.4f));
                else
                    yield return null;

                _promptText.text = "点击继续  ▶";
                yield return new WaitUntil(LeftClickThisFrame);
                yield return null; // skip double-trigger

                if (hasImage)
                    yield return StartCoroutine(FadeImage(_slideImage, 1f, 0f, 0.25f));
            }

            _promptText.text = "";
            if (!_keepBlackAfter)
                yield return StartCoroutine(FadeCanvasOut());
            Finish();
        }

        private IEnumerator FadeImage(Image img, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, t / duration);
                img.color = new Color(1f, 1f, 1f, a);
                yield return null;
            }
            img.color = new Color(1f, 1f, 1f, to);
        }

        // ── Text-only ─────────────────────────────────────────────────────

        private IEnumerator PlayRoutine(string[] lines)
        {
            _canvas.enabled      = true;
            ResetCanvasAlpha();
            _blackBg.color       = Color.black;
            _slideImage.enabled  = false;
            _captionBar.enabled  = false;
            _captionText.enabled = false;
            _bodyText.enabled    = true;
            _promptText.enabled  = true;
            _bodyText.color      = Color.white;
            _promptText.color    = new Color(0.55f, 0.55f, 0.55f, 1f);

            if (_videoClip != null)
            {
                _videoPlayer.clip = _videoClip;
                _videoPlayer.Play();
                yield return new WaitUntil(() =>
                    !_videoPlayer.isPlaying || AnyKeyThisFrame());
                _videoPlayer.Stop();
            }
            else
            {
                foreach (var line in lines)
                {
                    _bodyText.color  = Color.white;
                    _promptText.text = "";
                    yield return StartCoroutine(Typewrite(line));
                    _promptText.text = "[ 按任意键继续 ]";
                    yield return new WaitUntil(AnyKeyThisFrame);
                    yield return null;
                }
            }

            yield return StartCoroutine(FadeTextOut());
            _canvas.enabled = false;
            Finish();
        }

        private IEnumerator Typewrite(string line)
        {
            _bodyText.text = "";
            foreach (char c in line)
            {
                if (AnyKeyThisFrame()) { _bodyText.text = line; yield break; }
                _bodyText.text += c;
                yield return new WaitForSecondsRealtime(0.035f);
            }
        }

        private IEnumerator FadeTextOut()
        {
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - t / 0.6f;
                _bodyText.color   = new Color(1f, 1f, 1f, a);
                _promptText.color = new Color(0.55f, 0.55f, 0.55f, a);
                yield return null;
            }
        }

        private IEnumerator FadeCanvasOut()
        {
            float t = 0f;
            var group = _canvas.gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = _canvas.gameObject.AddComponent<CanvasGroup>();
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = 1f - t / 0.5f;
                yield return null;
            }
            group.alpha = 0f;
            _canvas.enabled = false;
        }

        // Ensure CanvasGroup is fully opaque — fixes invisible canvas after a prior FadeCanvasOut.
        private void ResetCanvasAlpha()
        {
            var group = _canvas.gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = _canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
        }

        private void Finish()
        {
            if (_nextScene != null)
                SceneManager.LoadScene(_nextScene);
            else
                _onComplete?.Invoke();
        }

        // ── Input helpers ─────────────────────────────────────────────────

        private static bool LeftClickThisFrame()
            => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        private static bool AnyKeyThisFrame()
            => Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

        // ── Canvas Build ──────────────────────────────────────────────────

        private void BuildCanvas()
        {
            var root = new GameObject("CutsceneCanvas");
            root.transform.SetParent(transform, false);

            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();

            // Black background
            _blackBg = MakeFullscreenImage(root.transform, "BG", Color.black);

            // Slide image — fullscreen, aspect preserved
            var imgGO   = new GameObject("SlideImage");
            imgGO.transform.SetParent(root.transform, false);
            _slideImage = imgGO.AddComponent<Image>();
            _slideImage.color          = Color.white;
            _slideImage.preserveAspect = true;
            var imgRT   = imgGO.GetComponent<RectTransform>();
            imgRT.anchorMin  = new Vector2(0.05f, 0.12f);
            imgRT.anchorMax  = new Vector2(0.95f, 0.95f);
            imgRT.offsetMin  = imgRT.offsetMax = Vector2.zero;

            // Caption bar — dark strip at bottom
            _captionBar = MakeFullscreenImage(root.transform, "CaptionBar",
                new Color(0f, 0f, 0f, 0.72f));
            var barRT   = _captionBar.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0f, 0f);
            barRT.anchorMax = new Vector2(1f, 0.12f);
            barRT.offsetMin = barRT.offsetMax = Vector2.zero;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Caption text — inside the bar
            _captionText = MakeText(root.transform, "Caption", font, 22,
                new Vector2(0f, 32f), new Vector2(1600f, 80f));
            _captionText.alignment = TextAnchor.MiddleCenter;

            // Click prompt — bottom right (anchored to bottom-right corner)
            _promptText = MakeText(root.transform, "Prompt", font, 18,
                Vector2.zero, new Vector2(260f, 36f));
            var promptRT = _promptText.GetComponent<RectTransform>();
            promptRT.anchorMin        = new Vector2(1f, 0f);
            promptRT.anchorMax        = new Vector2(1f, 0f);
            promptRT.pivot            = new Vector2(1f, 0f);
            promptRT.anchoredPosition = new Vector2(-30f, 24f);
            _promptText.alignment = TextAnchor.MiddleRight;
            _promptText.color     = new Color(0.85f, 0.85f, 0.85f, 1f);

            // Body text (text-only mode)
            _bodyText = MakeText(root.transform, "Body", font, 22,
                new Vector2(0f, 40f), new Vector2(1000f, 420f));
            _bodyText.lineSpacing = 1.5f;

            // VideoPlayer
            _videoPlayer = root.AddComponent<VideoPlayer>();
            _videoPlayer.renderMode  = VideoRenderMode.CameraFarPlane;
            _videoPlayer.playOnAwake = false;
        }

        private static Image MakeFullscreenImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return img;
        }

        private static Text MakeText(Transform parent, string name, Font font,
            int size, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t  = go.AddComponent<Text>();
            t.font      = font;
            t.fontSize  = size;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            var rt      = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = sizeDelta;
            return t;
        }
    }
}
