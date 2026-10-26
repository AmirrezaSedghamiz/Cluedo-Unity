using System.Collections.Generic;
using UnityEngine;

namespace TomorrowsCrime.UI.Styling
{
    /// <summary>
    /// Central visual language and design token repository for Tomorrow's Crime.
    /// Provides consistent colors, margins, fonts, and GUIStyle definitions across all interfaces.
    /// Atmospheric detective aesthetic tailored for Meridian House.
    /// </summary>
    public static class UITheme
    {
        // --- Color Palette ---
        public static readonly Color CharcoalBackground = new Color(0.09f, 0.10f, 0.11f, 0.96f); // #17191C
        public static readonly Color SlateSurface = new Color(0.14f, 0.15f, 0.17f, 0.94f);        // #23272C
        public static readonly Color SlateSurfaceLight = new Color(0.18f, 0.20f, 0.23f, 0.95f);   // #2E333B
        public static readonly Color PaperSurface = new Color(0.91f, 0.89f, 0.84f, 1.0f);        // #E9E2D5
        public static readonly Color MutedBrass = new Color(0.71f, 0.60f, 0.38f, 1.0f);          // #B69A61
        public static readonly Color BrassHover = new Color(0.82f, 0.71f, 0.48f, 1.0f);          // #D1B57A
        public static readonly Color DesaturatedTeal = new Color(0.43f, 0.61f, 0.60f, 1.0f);      // #6D9C98
        public static readonly Color RestrainedAmber = new Color(0.85f, 0.60f, 0.29f, 1.0f);      // #D99A4A
        public static readonly Color MutedRed = new Color(0.72f, 0.36f, 0.35f, 1.0f);             // #B85C58
        public static readonly Color TextPrimary = new Color(0.96f, 0.95f, 0.94f, 1.0f);         // #F5F3EF
        public static readonly Color TextSecondary = new Color(0.61f, 0.63f, 0.65f, 1.0f);       // #9BA0A6
        public static readonly Color TextDarkOnPaper = new Color(0.10f, 0.11f, 0.12f, 1.0f);     // #1A1C1F
        public static readonly Color BorderSubtle = new Color(0.25f, 0.27f, 0.30f, 0.80f);        // #40454D
        public static readonly Color SelectionHighlight = new Color(0.71f, 0.60f, 0.38f, 0.25f);

        // --- Texture Cache for solid background drawing ---
        private static readonly Dictionary<Color, Texture2D> _colorTextures = new();

        public static Texture2D GetSolidTexture(Color color)
        {
            if (_colorTextures.TryGetValue(color, out var tex) && tex != null)
            {
                return tex;
            }

            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            _colorTextures[color] = tex;
            return tex;
        }

        public static void DrawSolidRect(Rect rect, Color color)
        {
            var prevColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prevColor;
        }

        public static void DrawBorder(Rect rect, Color borderColor, float thickness = 1f)
        {
            // Top, Bottom, Left, Right
            DrawSolidRect(new Rect(rect.x, rect.y, rect.width, thickness), borderColor);
            DrawSolidRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), borderColor);
            DrawSolidRect(new Rect(rect.x, rect.y, thickness, rect.height), borderColor);
            DrawSolidRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), borderColor);
        }

        // --- Styles Lazy Initializers ---
        private static GUIStyle _titleStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _subHeaderStyle;
        private static GUIStyle _bodyStyle;
        private static GUIStyle _bodyMutedStyle;
        private static GUIStyle _paperBodyStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _primaryButtonStyle;
        private static GUIStyle _tabButtonStyle;
        private static GUIStyle _activeTabButtonStyle;
        private static GUIStyle _cardStyle;
        private static GUIStyle _cardSelectedStyle;
        private static GUIStyle _badgeStyle;

        public static GUIStyle TitleStyle
        {
            get
            {
                if (_titleStyle == null)
                {
                    _titleStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 24,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = MutedBrass },
                        alignment = TextAnchor.MiddleLeft
                    };
                }
                return _titleStyle;
            }
        }

        public static GUIStyle HeaderStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 18,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = TextPrimary },
                        alignment = TextAnchor.MiddleLeft
                    };
                }
                return _headerStyle;
            }
        }

        public static GUIStyle SubHeaderStyle
        {
            get
            {
                if (_subHeaderStyle == null)
                {
                    _subHeaderStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 14,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = MutedBrass },
                        alignment = TextAnchor.MiddleLeft
                    };
                }
                return _subHeaderStyle;
            }
        }

        public static GUIStyle BodyStyle
        {
            get
            {
                if (_bodyStyle == null)
                {
                    _bodyStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 13,
                        normal = { textColor = TextPrimary },
                        wordWrap = true,
                        richText = true
                    };
                }
                return _bodyStyle;
            }
        }

        public static GUIStyle BodyMutedStyle
        {
            get
            {
                if (_bodyMutedStyle == null)
                {
                    _bodyMutedStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        normal = { textColor = TextSecondary },
                        wordWrap = true,
                        richText = true
                    };
                }
                return _bodyMutedStyle;
            }
        }

        public static GUIStyle PaperBodyStyle
        {
            get
            {
                if (_paperBodyStyle == null)
                {
                    _paperBodyStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 13,
                        normal = { textColor = TextDarkOnPaper },
                        wordWrap = true,
                        richText = true
                    };
                }
                return _paperBodyStyle;
            }
        }

        public static GUIStyle ButtonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = TextPrimary, background = GetSolidTexture(SlateSurfaceLight) },
                        hover = { textColor = TextPrimary, background = GetSolidTexture(BorderSubtle) },
                        active = { textColor = MutedBrass, background = GetSolidTexture(CharcoalBackground) },
                        padding = new RectOffset(12, 12, 8, 8)
                    };
                }
                return _buttonStyle;
            }
        }

        public static GUIStyle PrimaryButtonStyle
        {
            get
            {
                if (_primaryButtonStyle == null)
                {
                    _primaryButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = CharcoalBackground, background = GetSolidTexture(MutedBrass) },
                        hover = { textColor = CharcoalBackground, background = GetSolidTexture(BrassHover) },
                        active = { textColor = TextPrimary, background = GetSolidTexture(RestrainedAmber) },
                        padding = new RectOffset(14, 14, 8, 8)
                    };
                }
                return _primaryButtonStyle;
            }
        }

        public static GUIStyle TabButtonStyle
        {
            get
            {
                if (_tabButtonStyle == null)
                {
                    _tabButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Normal,
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = TextSecondary, background = GetSolidTexture(SlateSurface) },
                        hover = { textColor = TextPrimary, background = GetSolidTexture(SlateSurfaceLight) },
                        active = { textColor = MutedBrass, background = GetSolidTexture(CharcoalBackground) },
                        padding = new RectOffset(16, 12, 10, 10)
                    };
                }
                return _tabButtonStyle;
            }
        }

        public static GUIStyle ActiveTabButtonStyle
        {
            get
            {
                if (_activeTabButtonStyle == null)
                {
                    _activeTabButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = MutedBrass, background = GetSolidTexture(CharcoalBackground) },
                        hover = { textColor = MutedBrass, background = GetSolidTexture(CharcoalBackground) },
                        active = { textColor = MutedBrass, background = GetSolidTexture(CharcoalBackground) },
                        padding = new RectOffset(16, 12, 10, 10)
                    };
                }
                return _activeTabButtonStyle;
            }
        }

        public static GUIStyle CardStyle
        {
            get
            {
                if (_cardStyle == null)
                {
                    _cardStyle = new GUIStyle(GUI.skin.box)
                    {
                        normal = { textColor = TextPrimary, background = GetSolidTexture(SlateSurface) },
                        padding = new RectOffset(12, 12, 10, 10),
                        margin = new RectOffset(0, 0, 4, 4)
                    };
                }
                return _cardStyle;
            }
        }

        public static GUIStyle CardSelectedStyle
        {
            get
            {
                if (_cardSelectedStyle == null)
                {
                    _cardSelectedStyle = new GUIStyle(GUI.skin.box)
                    {
                        normal = { textColor = TextPrimary, background = GetSolidTexture(SlateSurfaceLight) },
                        padding = new RectOffset(12, 12, 10, 10),
                        margin = new RectOffset(0, 0, 4, 4)
                    };
                }
                return _cardSelectedStyle;
            }
        }

        public static GUIStyle BadgeStyle
        {
            get
            {
                if (_badgeStyle == null)
                {
                    _badgeStyle = new GUIStyle(GUI.skin.box)
                    {
                        fontSize = 10,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = CharcoalBackground, background = GetSolidTexture(DesaturatedTeal) },
                        padding = new RectOffset(6, 6, 2, 2)
                    };
                }
                return _badgeStyle;
            }
        }
    }
}
