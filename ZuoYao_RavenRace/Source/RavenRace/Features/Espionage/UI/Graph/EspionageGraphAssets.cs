using UnityEngine;
using Verse;

namespace RavenRace.Features.Espionage.UI.Graph
{
    /// <summary>
    /// 负责加载和持有间谍界面所需的贴图资源。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class EspionageGraphAssets
    {
        public static readonly Texture2D IconUnknown;
        public static readonly Texture2D FrameNormal;
        public static readonly Texture2D FrameKnown;
        public static readonly Texture2D FrameTurncoat;

        public static readonly Color LineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        public static readonly Color KnownNameColor = new Color(1.0f, 0.8f, 0.3f); // 金色
        public static readonly Color TurncoatNameColor = Color.green;

        static EspionageGraphAssets()
        {
            IconUnknown = ContentFinder<Texture2D>.Get("UI/Icons/QuestionMark", false) ?? BaseContent.BadTex;

            // 使用纯色纹理作为边框背景，性能更好
            FrameNormal = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.2f));
            FrameKnown = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.3f, 0.1f));
            FrameTurncoat = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.4f, 0.1f));
        }
    }
}