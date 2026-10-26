using System.Collections.Generic;
using UnityEngine;

namespace TomorrowsCrime.UI.Styling
{
    /// <summary>
    /// Central visual language and design token repository for Tomorrow's Crime.
    /// Rebuilt for Step 7: Modern Cluedo Art Direction (Meridian House Aesthetic).
    /// Provides luxury jewel-tone colors, Art Deco ornamentation, procedural borders,
    /// serif typography, and tactile case-dossier styling across all UI screens.
    /// </summary>
    public static class UITheme
    {
        // --- 1. CORE JEWEL-TONE & LUXURY COLOR PALETTE ---
        // Brand & Backdrop
        public static readonly Color MeridianMidnight = new Color(0.05f, 0.10f, 0.16f, 0.98f);  // #0D1B2A - Deep midnight navy
        public static readonly Color ObsidianSurface = new Color(0.08f, 0.09f, 0.12f, 0.96f);    // #15181E - Deep slate obsidian
        public static readonly Color ObsidianSurfaceLight = new Color(0.12f, 0.14f, 0.18f, 0.96f);// #1F242E - Elevated panel surface
        public static readonly Color AgedParchment = new Color(0.95f, 0.92f, 0.86f, 1.0f);       // #F3EBDD - Case file paper
        public static readonly Color AgedParchmentLight = new Color(0.98f, 0.96f, 0.92f, 1.0f);  // #FAF5EB - Crisp letterhead

        // Metals & Accents
        public static readonly Color PolishedBrass = new Color(0.83f, 0.69f, 0.22f, 1.0f);       // #D4AF37 - Rich antique gold/brass
        public static readonly Color BrassHighlight = new Color(0.96f, 0.85f, 0.45f, 1.0f);      // #F5D973 - Bright polished gold shimmer
        public static readonly Color BrassDark = new Color(0.62f, 0.49f, 0.14f, 1.0f);           // #9E7D24 - Burnished deep brass shadow
        public static readonly Color PeacockTeal = new Color(0.11f, 0.31f, 0.36f, 1.0f);         // #1D4E5B - Peacock ribbon accent
        public static readonly Color BordeauxCrimson = new Color(0.42f, 0.11f, 0.18f, 1.0f);      // #6B1D2F - Velvet wine/burgundy
        public static readonly Color EmeraldGreen = new Color(0.11f, 0.26f, 0.20f, 1.0f);         // #1B4332 - Syndicate emerald

        // Typography Colors
        public static readonly Color TextIvory = new Color(0.98f, 0.97f, 0.95f, 1.0f);           // #FDFBF7 - High contrast on dark
        public static readonly Color TextGold = new Color(0.91f, 0.78f, 0.38f, 1.0f);            // #E8C761 - Headers and seals
        public static readonly Color TextMuted = new Color(0.65f, 0.68f, 0.72f, 1.0f);           // #A6AEB8 - Subdued captions
        public static readonly Color TextDarkInk = new Color(0.10f, 0.10f, 0.12f, 1.0f);         // #1A1A1E - Ink on parchment paper
        public static readonly Color TextDarkInkMuted = new Color(0.35f, 0.33f, 0.30f, 1.0f);     // #59544D - Subdued text on paper

        // Simulation Urgency & Status
        public static readonly Color StatusAmber = new Color(0.90f, 0.66f, 0.24f, 1.0f);          // #E5A93C - Normal / > 60s
        public static readonly Color StatusOrange = new Color(0.90f, 0.32f, 0.0f, 1.0f);           // #E65100 - Warning / 15-60s
        public static readonly Color StatusDanger = new Color(0.78f, 0.16f, 0.16f, 1.0f);          // #C62828 - Danger / < 15s
        public static readonly Color StatusSuccess = new Color(0.18f, 0.49f, 0.20f, 1.0f);         // #2E7D32 - Unlocked / verified

        // --- BACKWARD COMPATIBLE ALIASES ---
        public static Color CharcoalBackground => MeridianMidnight;
        public static Color SlateSurface => ObsidianSurface;
        public static Color SlateSurfaceLight => ObsidianSurfaceLight;
        public static Color PaperSurface => AgedParchment;
        public static Color MutedBrass => PolishedBrass;
        public static Color BrassHover => BrassHighlight;
        public static Color DesaturatedTeal => PeacockTeal;
        public static Color RestrainedAmber => StatusAmber;
        public static Color MutedRed => StatusDanger;
        public static Color TextPrimary => TextIvory;
        public static Color TextSecondary => TextMuted;
        public static Color TextDarkOnPaper => TextDarkInk;
        public static Color BorderSubtle => new Color(0.83f, 0.69f, 0.22f, 0.35f);
        public static Color SelectionHighlight => new Color(0.83f, 0.69f, 0.22f, 0.25f);

        // --- 2. TEXTURE CACHE & SPRITE ASSETS ---
        private static readonly Dictionary<Color, Texture2D> _colorTextures = new();
        private static Texture2D _pocketWatchTex;
        private static Texture2D _parchmentCardTex;
        private static Texture2D _waxSealTex;
        private static Texture2D _dossierFolderTex;

        public static Texture2D PocketWatchTexture => _pocketWatchTex ??= LoadTexture("UI/Sprites/UI_PocketWatch_Dial.png");
        public static Texture2D ParchmentCardTexture => _parchmentCardTex ??= LoadTexture("UI/Sprites/UI_Parchment_Card.png");
        public static Texture2D WaxSealTexture => _waxSealTex ??= LoadTexture("UI/Sprites/UI_WaxSeal_Gold.png");
        public static Texture2D DossierFolderTexture => _dossierFolderTex ??= LoadTexture("UI/Sprites/UI_Dossier_Folder.png");

        private static Texture2D LoadTexture(string relativePath)
        {
            string fullPath = System.IO.Path.Combine(Application.dataPath, "_Project", relativePath);
            if (System.IO.File.Exists(fullPath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes)) return tex;
            }
            return null;
        }

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
            DrawSolidRect(new Rect(rect.x, rect.y, rect.width, thickness), borderColor);
            DrawSolidRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), borderColor);
            DrawSolidRect(new Rect(rect.x, rect.y, thickness, rect.height), borderColor);
            DrawSolidRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), borderColor);
        }

        // --- 3. ART DECO ORNAMENTATION & BESPOKE RENDERING ---
        public static void DrawOrnateFrame(Rect rect, Color borderColor, Color accentColor, float thickness = 2f)
        {
            // Outer Border
            DrawBorder(rect, borderColor, thickness);

            // Inner Accent Border (offset 4px)
            if (rect.width > 20f && rect.height > 20f)
            {
                Rect innerRect = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
                DrawBorder(innerRect, new Color(accentColor.r, accentColor.g, accentColor.b, 0.45f), 1f);
            }

            // Stepped Art Deco Corners
            float cornerSize = Mathf.Min(16f, Mathf.Min(rect.width * 0.2f, rect.height * 0.2f));
            DrawSolidRect(new Rect(rect.x, rect.y, cornerSize, 3f), accentColor);
            DrawSolidRect(new Rect(rect.x, rect.y, 3f, cornerSize), accentColor);

            DrawSolidRect(new Rect(rect.xMax - cornerSize, rect.y, cornerSize, 3f), accentColor);
            DrawSolidRect(new Rect(rect.xMax - 3f, rect.y, 3f, cornerSize), accentColor);

            DrawSolidRect(new Rect(rect.x, rect.yMax - 3f, cornerSize, 3f), accentColor);
            DrawSolidRect(new Rect(rect.x, rect.yMax - cornerSize, 3f, cornerSize), accentColor);

            DrawSolidRect(new Rect(rect.xMax - cornerSize, rect.yMax - 3f, cornerSize, 3f), accentColor);
            DrawSolidRect(new Rect(rect.xMax - 3f, rect.yMax - cornerSize, 3f, cornerSize), accentColor);
        }

        public static void DrawParchmentCard(Rect rect)
        {
            if (ParchmentCardTexture != null)
            {
                GUI.DrawTexture(rect, ParchmentCardTexture, ScaleMode.StretchToFill);
            }
            else
            {
                DrawSolidRect(rect, AgedParchment);
                DrawOrnateFrame(rect, PolishedBrass, BrassDark, 2f);
            }
        }

        public static void DrawWaxSeal(Rect rect)
        {
            if (WaxSealTexture != null)
            {
                GUI.DrawTexture(rect, WaxSealTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                DrawSolidRect(rect, PolishedBrass);
            }
        }

        public static void DrawGoldHeader(Rect rect, string title, string subtitle = null)
        {
            // Decorative gold header plaque
            DrawSolidRect(rect, ObsidianSurface);
            DrawBorder(rect, PolishedBrass, 1.5f);

            // Corner accents
            DrawSolidRect(new Rect(rect.x, rect.y, 10f, 2f), BrassHighlight);
            DrawSolidRect(new Rect(rect.xMax - 10f, rect.y, 10f, 2f), BrassHighlight);

            Rect titleRect = new Rect(rect.x + 16f, rect.y + 6f, rect.width - 32f, rect.height - 12f);
            if (string.IsNullOrEmpty(subtitle))
            {
                GUI.Label(titleRect, title, TitleStyle);
            }
            else
            {
                GUI.Label(new Rect(rect.x + 16f, rect.y + 4f, rect.width - 32f, 24f), title, TitleStyle);
                GUI.Label(new Rect(rect.x + 16f, rect.y + 26f, rect.width - 32f, 18f), subtitle, SubHeaderStyle);
            }
        }

        public static bool DrawLuxeButton(Rect rect, string text, bool isPrimary = false)
        {
            bool isHovered = rect.Contains(Event.current.mousePosition);
            Color bgColor = isPrimary
                ? (isHovered ? BrassHighlight : PolishedBrass)
                : (isHovered ? ObsidianSurfaceLight : ObsidianSurface);

            Color textColor = isPrimary ? MeridianMidnight : (isHovered ? BrassHighlight : TextIvory);
            Color borderColor = isPrimary ? PolishedBrass : (isHovered ? BrassHighlight : BorderSubtle);

            DrawSolidRect(rect, bgColor);
            DrawOrnateFrame(rect, borderColor, isHovered ? BrassHighlight : PolishedBrass, isPrimary ? 2f : 1f);

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = isPrimary ? 14 : 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor }
            };

            GUI.Label(rect, text, style);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        // --- 4. FIRST-PERSON HUD VISUALS ---
        public static void DrawPocketWatchClock(Rect rect, float elapsed, float remaining, int loopIndex, float progress)
        {
            // Outer Plaque
            DrawSolidRect(rect, ObsidianSurface);
            DrawOrnateFrame(rect, PolishedBrass, BrassDark, 2f);

            // Left: Ornate Pocket Watch Dial
            float dialSize = rect.height - 10f;
            Rect dialRect = new Rect(rect.x + 8f, rect.y + 5f, dialSize, dialSize);
            if (PocketWatchTexture != null)
            {
                GUI.DrawTexture(dialRect, PocketWatchTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                DrawSolidRect(dialRect, PolishedBrass);
            }

            // Center: Loop Counter Badge & Time Remaining Readout
            float textX = dialRect.xMax + 14f;
            float textWidth = rect.width - dialSize - 32f;

            Color timeColor = PolishedBrass;
            string urgencyLabel = "NORMAL OPERATION";
            if (remaining <= 15f)
            {
                timeColor = (Mathf.Sin(Time.time * 8f) > 0f) ? StatusDanger : TextIvory;
                urgencyLabel = "TEMPORAL COLLAPSE IMMINENT";
            }
            else if (remaining <= 60f)
            {
                timeColor = StatusOrange;
                urgencyLabel = "LOOP EXPIRING";
            }

            int remMin = (int)(remaining / 60f);
            int remSec = (int)(remaining % 60f);
            string timeStr = $"{remMin:D2}:{remSec:D2}";

            var timeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = timeColor }
            };
            GUI.Label(new Rect(textX, rect.y + 6f, textWidth, 26f), timeStr, timeStyle);

            var metaStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = TextMuted }
            };
            GUI.Label(new Rect(textX, rect.y + 30f, textWidth, 16f), $"LOOP {loopIndex} • <color=#{ColorUtility.ToHtmlStringRGB(timeColor)}>{urgencyLabel}</color>", metaStyle);

            // Bottom Progress Bar
            float barHeight = 4f;
            Rect barBg = new Rect(rect.x + 4f, rect.yMax - barHeight - 4f, rect.width - 8f, barHeight);
            DrawSolidRect(barBg, MeridianMidnight);
            Rect barFill = new Rect(rect.x + 4f, rect.yMax - barHeight - 4f, (rect.width - 8f) * progress, barHeight);
            DrawSolidRect(barFill, timeColor);
        }

        public static void DrawDiamondReticle(Vector2 center, bool isTargeting, string prompt = null)
        {
            float size = isTargeting ? 18f : 8f;
            Color reticleColor = isTargeting ? BrassHighlight : PolishedBrass;

            // Diamond Center Reticle
            Rect diamondRect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            DrawBorder(diamondRect, reticleColor, 1.5f);

            if (isTargeting)
            {
                // Art Deco Corner Brackets
                float bOffset = 16f;
                DrawSolidRect(new Rect(center.x - bOffset, center.y - bOffset, 6f, 2f), BrassHighlight);
                DrawSolidRect(new Rect(center.x - bOffset, center.y - bOffset, 2f, 6f), BrassHighlight);

                DrawSolidRect(new Rect(center.x + bOffset - 6f, center.y - bOffset, 6f, 2f), BrassHighlight);
                DrawSolidRect(new Rect(center.x + bOffset - 2f, center.y - bOffset, 2f, 6f), BrassHighlight);

                DrawSolidRect(new Rect(center.x - bOffset, center.y + bOffset - 2f, 6f, 2f), BrassHighlight);
                DrawSolidRect(new Rect(center.x - bOffset, center.y + bOffset - 6f, 2f, 6f), BrassHighlight);

                DrawSolidRect(new Rect(center.x + bOffset - 6f, center.y + bOffset - 2f, 6f, 2f), BrassHighlight);
                DrawSolidRect(new Rect(center.x + bOffset - 2f, center.y + bOffset - 6f, 2f, 6f), BrassHighlight);

                // Prompt Plaque below
                if (!string.IsNullOrEmpty(prompt))
                {
                    float pWidth = 240f;
                    float pHeight = 28f;
                    Rect promptRect = new Rect(center.x - pWidth * 0.5f, center.y + 24f, pWidth, pHeight);
                    DrawSolidRect(promptRect, ObsidianSurface);
                    DrawOrnateFrame(promptRect, PolishedBrass, BrassHighlight, 1f);

                    var pStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = TextIvory }
                    };
                    GUI.Label(promptRect, prompt.ToUpper(), pStyle);
                }
            }
        }

        // --- 5. STYLES DEFINITIONS ---
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

        public static GUIStyle TitleStyle => _titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = PolishedBrass },
            alignment = TextAnchor.MiddleLeft
        };

        public static GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = TextIvory },
            alignment = TextAnchor.MiddleLeft
        };

        public static GUIStyle SubHeaderStyle => _subHeaderStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = PolishedBrass },
            alignment = TextAnchor.MiddleLeft
        };

        public static GUIStyle BodyStyle => _bodyStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = TextIvory },
            wordWrap = true,
            richText = true
        };

        public static GUIStyle BodyMutedStyle => _bodyMutedStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = TextMuted },
            wordWrap = true,
            richText = true
        };

        public static GUIStyle PaperBodyStyle => _paperBodyStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = TextDarkInk },
            wordWrap = true,
            richText = true
        };

        public static GUIStyle ButtonStyle => _buttonStyle ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = TextIvory, background = GetSolidTexture(ObsidianSurfaceLight) },
            hover = { textColor = BrassHighlight, background = GetSolidTexture(ObsidianSurface) },
            active = { textColor = PolishedBrass, background = GetSolidTexture(MeridianMidnight) },
            padding = new RectOffset(12, 12, 8, 8)
        };

        public static GUIStyle PrimaryButtonStyle => _primaryButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = MeridianMidnight, background = GetSolidTexture(PolishedBrass) },
            hover = { textColor = MeridianMidnight, background = GetSolidTexture(BrassHighlight) },
            active = { textColor = TextIvory, background = GetSolidTexture(StatusAmber) },
            padding = new RectOffset(14, 14, 8, 8)
        };

        public static GUIStyle TabButtonStyle => _tabButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = TextMuted, background = GetSolidTexture(ObsidianSurface) },
            hover = { textColor = TextIvory, background = GetSolidTexture(ObsidianSurfaceLight) },
            active = { textColor = PolishedBrass, background = GetSolidTexture(MeridianMidnight) },
            padding = new RectOffset(16, 12, 10, 10)
        };

        public static GUIStyle ActiveTabButtonStyle => _activeTabButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = PolishedBrass, background = GetSolidTexture(MeridianMidnight) },
            hover = { textColor = BrassHighlight, background = GetSolidTexture(MeridianMidnight) },
            active = { textColor = PolishedBrass, background = GetSolidTexture(MeridianMidnight) },
            padding = new RectOffset(16, 12, 10, 10)
        };

        public static GUIStyle CardStyle => _cardStyle ??= new GUIStyle(GUI.skin.box)
        {
            normal = { textColor = TextIvory, background = GetSolidTexture(ObsidianSurface) },
            padding = new RectOffset(12, 12, 10, 10),
            margin = new RectOffset(0, 0, 4, 4)
        };

        public static GUIStyle CardSelectedStyle => _cardSelectedStyle ??= new GUIStyle(GUI.skin.box)
        {
            normal = { textColor = TextIvory, background = GetSolidTexture(ObsidianSurfaceLight) },
            padding = new RectOffset(12, 12, 10, 10),
            margin = new RectOffset(0, 0, 4, 4)
        };

        public static GUIStyle BadgeStyle => _badgeStyle ??= new GUIStyle(GUI.skin.box)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = MeridianMidnight, background = GetSolidTexture(PolishedBrass) },
            padding = new RectOffset(6, 6, 2, 2)
        };
    }
}
