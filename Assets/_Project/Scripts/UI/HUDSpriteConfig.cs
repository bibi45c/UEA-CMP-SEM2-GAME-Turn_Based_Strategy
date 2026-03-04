using UnityEngine;

namespace TurnBasedTactics.UI
{
    /// <summary>
    /// ScriptableObject holding references to Synty Dark Fantasy HUD sprites.
    /// Assign sprites in Unity Editor. When null, UI falls back to code-drawn rectangles.
    ///
    /// Recommended sprite assignments from InterfaceDarkFantasyHUD:
    ///   slotFrame        → SPR_DarkFantasy_Box_Hotbar_01 (or _02/_03/_04)
    ///   slotBackground   → SPR_DarkFantasy_Frame_Arch_Large_01_Background
    ///   slotHighlight    → SPR_DarkFantasy_Box_Hotbar_02 (brighter variant)
    ///   panelBackground  → SPR_DarkFantasy_Frame_Arch_Large_01_Background
    ///   panelFrame       → SPR_DarkFantasy_Frame_Arch_Large_01 (9-slice)
    ///   hpBarFrame       → SPR_DarkFantasy_Bar_01_Top (or Bar_02_Top)
    ///   hpBarFill        → SPR_DarkFantasy_Bar_05 (or Bar_06/Bar_08)
    ///   hpBarBackground  → SPR_DarkFantasy_Bar_01_Bottom
    ///   hpBarVignette    → (optional, use FX sprites for depth)
    ///   apGemFull        → SPR_DarkFantasy_Frame_Orb_01_Glass (tint green)
    ///   apGemEmpty       → SPR_DarkFantasy_Frame_Orb_01_Glass (tint dark)
    ///   apGemContainer   → SPR_DarkFantasy_Frame_Orb_01_Left + _Right
    ///   portraitFrame    → SPR_DarkFantasy_Frame_Arch_Medium_01 (or similar)
    ///   turnSlotFrame    → SPR_DarkFantasy_Box_Hotbar_01
    ///   banner           → (use Frame_Arch sprites as banner alternative)
    ///   shadowSprite     → (use FX/Gradient sprites if available)
    ///   separatorLine    → SPR_DarkFantasy_Bar_04_Left or _Right
    ///   curlicue         → (check Greebles folder for decorative elements)
    ///   iconPlaceholder  → (use Icons_Inventory empty slot sprites)
    ///   hudFont          → Fonts/Cinzel/Cinzel-Bold (or similar gothic font)
    /// </summary>
    [CreateAssetMenu(fileName = "HUDSpriteConfig", menuName = "TurnBasedTactics/HUD Sprite Config")]
    public class HUDSpriteConfig : ScriptableObject
    {
        [Header("Action Bar — Slot Frames")]
        [Tooltip("9-slice frame for ability/action slots")]
        public Sprite SlotFrame;
        [Tooltip("Dark background fill for slots")]
        public Sprite SlotBackground;
        [Tooltip("Bright frame for hovered/selected slots")]
        public Sprite SlotHighlight;

        [Header("Panels")]
        [Tooltip("Dark background for panels (turn order, portrait, hotbar)")]
        public Sprite PanelBackground;
        [Tooltip("Decorative frame border for panels")]
        public Sprite PanelFrame;
        [Tooltip("Drop shadow for panels")]
        public Sprite ShadowSprite;

        [Header("Health Bars")]
        [Tooltip("9-slice frame around HP bar")]
        public Sprite HPBarFrame;
        [Tooltip("Fill sprite for HP bar (stretched horizontally)")]
        public Sprite HPBarFill;
        [Tooltip("Dark background behind HP bar fill")]
        public Sprite HPBarBackground;
        [Tooltip("Vignette overlay for health bar depth")]
        public Sprite HPBarVignette;

        [Header("Action Points")]
        [Tooltip("Filled AP gem (green/available)")]
        public Sprite APGemFull;
        [Tooltip("Empty AP gem background (spent)")]
        public Sprite APGemEmpty;
        [Tooltip("Ornamental container around AP gem")]
        public Sprite APGemContainer;

        [Header("Portraits & Turn Order")]
        [Tooltip("Decorative frame for party portraits")]
        public Sprite PortraitFrame;
        [Tooltip("Frame for turn order slots")]
        public Sprite TurnSlotFrame;

        [Header("Decorative")]
        [Tooltip("Banner sprite for round/header display")]
        public Sprite Banner;
        [Tooltip("Vertical/horizontal separator line")]
        public Sprite SeparatorLine;
        [Tooltip("Ornamental curlicue decoration")]
        public Sprite Curlicue;
        [Tooltip("Placeholder icon for empty ability slots")]
        public Sprite IconPlaceholder;

        [Header("Font")]
        [Tooltip("Gothic-themed font (Cinzel-Bold or similar dark fantasy font)")]
        public Font HUDFont;

        /// <summary>Whether all critical sprites are assigned.</summary>
        public bool IsValid => SlotFrame != null && PanelBackground != null;
    }
}
