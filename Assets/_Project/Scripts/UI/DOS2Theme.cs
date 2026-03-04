using UnityEngine;
using UnityEngine.UI;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// Static color palette and UI factory methods for DOS2-style combat HUD.
    /// When a HUDSpriteConfig is set, factory methods use Synty sprites;
    /// otherwise falls back to code-drawn colored rectangles.
    /// </summary>
    public static class DOS2Theme
    {
        // ── Sprite Config (set by GameBootstrap) ──────────────────────
        public static HUDSpriteConfig Sprites { get; set; }
        public static bool HasSprites => Sprites != null && Sprites.IsValid;

        // ── Core Palette ──────────────────────────────────────────────
        public static readonly Color GoldAccent     = HexColor("#B8860B"); // Darker gold for Dark Fantasy
        public static readonly Color GoldHighlight  = HexColor("#DAA520"); // Goldenrod highlight
        public static readonly Color PanelBg        = HexColor("#0A0A0F"); // Deeper black-purple
        public static readonly Color PanelBgAlt     = HexColor("#151520"); // Slightly lighter dark
        public static readonly Color PartyBlue      = HexColor("#4169E1"); // Royal blue (less bright)
        public static readonly Color EnemyRed       = HexColor("#8B0000"); // Dark red (blood)
        public static readonly Color AllyGreen      = HexColor("#228B22"); // Forest green
        public static readonly Color APGreen        = HexColor("#32CD32"); // Lime green (slightly muted)
        public static readonly Color APUsedRed      = HexColor("#8B0000"); // Match enemy red
        public static readonly Color TextWhite      = HexColor("#E8E8E8"); // Slightly off-white
        public static readonly Color TextGray       = HexColor("#8B8B8B"); // Darker gray
        public static readonly Color HPGreen        = new Color(0.13f, 0.55f, 0.13f, 1f); // Forest green
        public static readonly Color HPYellow       = new Color(0.85f, 0.65f, 0.13f, 1f); // Dark goldenrod
        public static readonly Color HPRed          = new Color(0.55f, 0f, 0f, 1f); // Dark red

        // Dark Fantasy palette — gothic tones matching the asset pack
        public static readonly Color SyntyDarkBg    = HexColor("#1A0F1F"); // Deep purple-black
        public static readonly Color SyntyGold      = HexColor("#B8860B"); // Dark goldenrod
        public static readonly Color SyntyFrameGray = HexColor("#4A4A4A"); // Darker gray frame

        // ── Semi-transparent variants ─────────────────────────────────
        public static readonly Color PanelBg85      = WithAlpha(PanelBg, 0.85f);
        public static readonly Color PanelBg70      = WithAlpha(PanelBg, 0.70f);
        public static readonly Color SlotBgDark     = WithAlpha(PanelBgAlt, 0.90f);
        public static readonly Color InactiveBorder = new Color(0.27f, 0.27f, 0.27f, 0.6f);

        // ── Font Helper ───────────────────────────────────────────────

        /// <summary>Returns the HUD font from config, or the built-in fallback.</summary>
        public static Font GetFont()
        {
            if (Sprites != null && Sprites.HUDFont != null)
                return Sprites.HUDFont;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // ── Factory Methods ───────────────────────────────────────────

        /// <summary>Create a UI element with RectTransform under the given parent.</summary>
        public static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Create a panel Image with given color, stretched to fill parent.</summary>
        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// Create a sprite-backed Image. Uses the given sprite with sliced mode
        /// if available, otherwise falls back to a solid color rectangle.
        /// </summary>
        public static Image CreateSpriteImage(
            string name, Transform parent,
            Sprite sprite, Color tint,
            bool sliced = true, float pixelsPerUnit = 1f)
        {
            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
                img.pixelsPerUnitMultiplier = pixelsPerUnit;
            }
            img.color = tint;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Create a bordered frame: outer image acts as border, inner fill inset by borderWidth.</summary>
        public static (Image border, Image fill) CreateBorderedPanel(
            string name, Transform parent,
            Color borderColor, Color fillColor, float borderWidth = 2f)
        {
            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var borderImg = go.AddComponent<Image>();
            borderImg.color = borderColor;

            var innerGO = CreateUIElement("Fill", go.transform);
            var innerRect = innerGO.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(borderWidth, borderWidth);
            innerRect.offsetMax = new Vector2(-borderWidth, -borderWidth);

            var fillImg = innerGO.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.raycastTarget = false;

            return (borderImg, fillImg);
        }

        /// <summary>Create a Text component with DOS2/Synty styling.</summary>
        public static Text CreateText(
            string name, Transform parent,
            string content, int fontSize,
            Color color, TextAnchor alignment = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Normal)
        {
            var go = CreateUIElement(name, parent);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = GetFont();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Create text with an outline effect for readability.</summary>
        public static Text CreateOutlinedText(
            string name, Transform parent,
            string content, int fontSize,
            Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var text = CreateText(name, parent, content, fontSize, color, alignment);
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        /// <summary>
        /// Create a shadow image behind a UI element (if shadow sprite is available).
        /// Returns null if no shadow sprite configured.
        /// </summary>
        public static Image CreateShadow(string name, Transform parent, float expand = 8f)
        {
            if (Sprites == null || Sprites.ShadowSprite == null) return null;

            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-expand, -expand);
            rect.offsetMax = new Vector2(expand, expand);

            var img = go.AddComponent<Image>();
            img.sprite = Sprites.ShadowSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.05f, 0.05f, 0.05f, 0.6f);
            img.raycastTarget = false;

            // Ensure shadow renders behind siblings
            go.transform.SetAsFirstSibling();
            return img;
        }

        /// <summary>Get HP bar color based on HP ratio (green → yellow → red).</summary>
        public static Color GetHPColor(float ratio)
        {
            if (ratio > 0.6f) return HPGreen;
            if (ratio > 0.3f) return Color.Lerp(HPYellow, HPGreen, (ratio - 0.3f) / 0.3f);
            return Color.Lerp(HPRed, HPYellow, ratio / 0.3f);
        }

        // ── Utility ───────────────────────────────────────────────────

        public static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        public static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }
    }
}
