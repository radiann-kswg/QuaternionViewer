using UnityEngine;
using UnityEngine.UIElements;

namespace QuaternionViewer.UI
{
    /// <summary>
    /// HUD 共通スタイル ―― 電子黒板 / シミュレータ端末風の情報パネル外観。
    /// 全パネル (Readout / ModelSwitcher / DemoToggles / GraphPlotter / Captions) で共有する。
    /// </summary>
    /// <remarks>濃紺の半透明地にシアンのアクセント枠・見出し。UXML/USS を使わず C# のみで統一する。</remarks>
    public static class HudStyle
    {
        public static readonly Color PanelBg = new Color(0.024f, 0.055f, 0.086f, 0.90f);
        public static readonly Color Accent = new Color(0.23f, 0.91f, 0.85f);
        public static readonly Color AccentDim = new Color(0.23f, 0.91f, 0.85f, 0.35f);
        public static readonly Color TextMain = new Color(0.84f, 0.90f, 0.93f);
        public static readonly Color TextDim = new Color(0.44f, 0.57f, 0.63f);
        public static readonly Color Track = new Color(0.09f, 0.16f, 0.22f);
        public static readonly Color ButtonActive = new Color(0.10f, 0.42f, 0.44f);

        private static Font _pixelFont;

        /// <summary>
        /// HUD 共通のピクセルフォント。患者長ひっく氏「マルモニカ (x12y16pxMaruMonica)」
        /// (英数・かな・漢字・ギリシャ文字収録)。表記は Fonts/x0y0pxFreeFont-NOTICE.md 参照。
        /// </summary>
        public static Font PixelFont
        {
            get
            {
                if (_pixelFont == null)
                {
                    _pixelFont = Resources.Load<Font>("Fonts/x12y16pxMaruMonica");
                }

                return _pixelFont;
            }
        }

        /// <summary>要素とその子孫へピクセルフォントを適用する (スタイル継承)。</summary>
        public static void ApplyFont(VisualElement ve)
        {
            Font f = PixelFont;
            if (f != null)
            {
                ve.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(f));
            }
        }

        private static Font _latinFont;

        /// <summary>
        /// 英数・記号用の幅広ピクセルフォント。同氏「スキャンライン (x12y20pxScanLine)」。
        /// 漢字非収録のため、ASCII+ギリシャ文字+°等だけの要素に使う (数値・見出し・ボタン)。
        /// </summary>
        public static Font LatinFont
        {
            get
            {
                if (_latinFont == null)
                {
                    _latinFont = Resources.Load<Font>("Fonts/x12y20pxScanLine");
                }

                return _latinFont;
            }
        }

        /// <summary>英数・記号のみの要素へ幅広フォントを適用する。</summary>
        public static void ApplyLatinFont(VisualElement ve)
        {
            Font f = LatinFont;
            if (f != null)
            {
                ve.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(f));
            }
        }

        /// <summary>パネルの枠・地・角丸・上端のアクセントライン・共通フォントを適用する。</summary>
        public static void Frame(VisualElement panel)
        {
            ApplyFont(panel);
            panel.style.backgroundColor = PanelBg;
            panel.style.borderTopWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderLeftWidth = 1f;
            panel.style.borderRightWidth = 1f;
            panel.style.borderTopColor = AccentDim;
            panel.style.borderBottomColor = AccentDim;
            panel.style.borderLeftColor = AccentDim;
            panel.style.borderRightColor = AccentDim;
            panel.style.borderTopLeftRadius = 3f;
            panel.style.borderTopRightRadius = 3f;
            panel.style.borderBottomLeftRadius = 3f;
            panel.style.borderBottomRightRadius = 3f;

            // 上端のアクセントライン (計器パネルのヘッダバー)
            var strip = new VisualElement();
            strip.style.position = Position.Absolute;
            strip.style.top = 0f;
            strip.style.left = 0f;
            strip.style.right = 0f;
            strip.style.height = 2f;
            strip.style.backgroundColor = Accent;
            strip.pickingMode = PickingMode.Ignore;
            panel.Add(strip);
        }

        /// <summary>見出しラベルをアクセント色の端末風にする (英数のみ想定・幅広字体)。</summary>
        public static void Header(Label label)
        {
            ApplyLatinFont(label);
            label.style.color = Accent;
            label.style.fontSize = 13;
            label.style.letterSpacing = 1.5f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        /// <summary>テーマ既定の余白・境界をリセットした端末風ボタンにする (英数のみ想定・幅広字体)。</summary>
        public static void Button(Button b, float height = 24f)
        {
            ApplyLatinFont(b);
            b.style.height = height;
            b.style.marginTop = 0f;
            b.style.marginBottom = 0f;
            b.style.marginLeft = 2f;
            b.style.marginRight = 2f;
            b.style.paddingTop = 0f;
            b.style.paddingBottom = 0f;
            b.style.paddingLeft = 8f;
            b.style.paddingRight = 8f;
            b.style.borderTopWidth = 1f;
            b.style.borderBottomWidth = 1f;
            b.style.borderLeftWidth = 1f;
            b.style.borderRightWidth = 1f;
            b.style.borderTopColor = AccentDim;
            b.style.borderBottomColor = AccentDim;
            b.style.borderLeftColor = AccentDim;
            b.style.borderRightColor = AccentDim;
            b.style.borderTopLeftRadius = 2f;
            b.style.borderTopRightRadius = 2f;
            b.style.borderBottomLeftRadius = 2f;
            b.style.borderBottomRightRadius = 2f;
            b.style.fontSize = 13;
            b.style.color = TextMain;
            b.style.backgroundColor = Track;
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        /// <summary>トグル系ボタンのアクティブ表示。</summary>
        public static void SetButtonActive(Button b, bool active)
        {
            b.style.backgroundColor = active ? ButtonActive : Track;
            b.style.color = active ? Accent : TextMain;
        }
    }
}
