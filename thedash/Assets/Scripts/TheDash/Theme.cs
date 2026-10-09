using UnityEngine;

namespace TheDash
{
    /// <summary>
    /// A colour palette for one "zone" of the run. Every visual in the game (sky, parallax
    /// layers, ground, hazards) is tinted from the active theme, so the whole scene always
    /// matches and zones blend smoothly into each other.
    /// </summary>
    public struct Theme
    {
        public string name;
        public Color skyTop, skyBottom, sun, far, mid, near, groundFill, groundEdge, hazard, stars;

        static Color H(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        Theme(string name, string skyTop, string skyBottom, string sun, string far, string mid, string near,
              string groundFill, string groundEdge, string hazard, float stars)
        {
            this.name = name;
            this.skyTop = H(skyTop);
            this.skyBottom = H(skyBottom);
            this.sun = H(sun);
            this.far = H(far);
            this.mid = H(mid);
            this.near = H(near);
            this.groundFill = H(groundFill);
            this.groundEdge = H(groundEdge);
            this.hazard = H(hazard);
            this.stars = new Color(1f, 1f, 1f, stars);
        }

        public static readonly Theme[] All =
        {
            new Theme("SUNSET DRIVE", "#2B1055", "#FF7E5F", "#FFD86F", "#9C3D86", "#5E1F6E", "#2E0F45", "#1A0B2E", "#FF4FD8", "#3DF2FF", 0.55f),
            new Theme("NEON CITY",    "#050B2E", "#5A189A", "#F72585", "#3F37C9", "#2B2290", "#14114D", "#0B0A2A", "#4CC9F0", "#FF3D8B", 1.00f),
            new Theme("AURORA PEAKS", "#021B2B", "#0B7A75", "#C8FFF4", "#11606B", "#093E4A", "#052A33", "#03181F", "#4DFFB4", "#FF5D8F", 0.90f),
            new Theme("EMBER RIDGE",  "#1A0505", "#C2410C", "#FFC145", "#7A2412", "#4F150C", "#2B0906", "#160403", "#FF9E3D", "#FFF06B", 0.35f),
            new Theme("SKY HIGH",     "#3A6FD8", "#FBC2EB", "#FFFFFF", "#8EA7E9", "#6A7FDB", "#4B5BA6", "#252C66", "#FFFFFF", "#FF3D7F", 0.00f),
        };

        public static Theme Lerp(Theme a, Theme b, float t)
        {
            return new Theme
            {
                name = t < 0.5f ? a.name : b.name,
                skyTop = Color.Lerp(a.skyTop, b.skyTop, t),
                skyBottom = Color.Lerp(a.skyBottom, b.skyBottom, t),
                sun = Color.Lerp(a.sun, b.sun, t),
                far = Color.Lerp(a.far, b.far, t),
                mid = Color.Lerp(a.mid, b.mid, t),
                near = Color.Lerp(a.near, b.near, t),
                groundFill = Color.Lerp(a.groundFill, b.groundFill, t),
                groundEdge = Color.Lerp(a.groundEdge, b.groundEdge, t),
                hazard = Color.Lerp(a.hazard, b.hazard, t),
                stars = Color.Lerp(a.stars, b.stars, t),
            };
        }
    }

    /// <summary>Shared UI colours, so menus match the in-game neon look.</summary>
    public static class UIColors
    {
        public static readonly Color Panel = new Color(0.09f, 0.05f, 0.19f, 1f);
        public static readonly Color PanelLight = new Color(0.16f, 0.10f, 0.32f, 1f);
        public static readonly Color Dim = new Color(0.02f, 0f, 0.06f, 0.72f);
        public static readonly Color Pink = new Color(1f, 0.31f, 0.85f);
        public static readonly Color Cyan = new Color(0.24f, 0.95f, 1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        public static readonly Color Green = new Color(0.30f, 1f, 0.62f);
        public static readonly Color Red = new Color(1f, 0.33f, 0.40f);
        public static readonly Color TextSoft = new Color(0.78f, 0.74f, 0.95f);
        public static readonly Color Button = new Color(0.26f, 0.16f, 0.52f);
    }
}
