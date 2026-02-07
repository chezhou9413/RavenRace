using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.Operator.Rewards;

namespace RavenRace.Features.Operator.UI
{
    [StaticConstructorOnStartup]
    public class Dialog_UnderwearCollection : Window
    {
        private List<ThingDef> allCollectibles;
        private HashSet<string> collectedDefs;
        private static readonly Texture2D lockedIcon = ContentFinder<Texture2D>.Get("UI/Operator/Collection/Locked", true);
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(700, 550);

        public Dialog_UnderwearCollection()
        {
            forcePause = true;
            doCloseX = true;
            doCloseButton = true;

            allCollectibles = DefDatabase<RewardDef>.AllDefs.Select(r => r.rewardThing).OrderBy(t => t.defName).ToList();
            collectedDefs = Find.World.GetComponent<WorldComponent_OperatorManager>().collectedUnderwearDefs;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35), "左爻的秘密衣橱");
            Text.Font = GameFont.Small;

            Rect contentRect = new Rect(inRect.x, inRect.y + 40, inRect.width, inRect.height - 80);

            float cardWidth = 128f;
            float cardHeight = 128f;
            float padding = 15f;

            int cardsPerRow = Mathf.FloorToInt((contentRect.width - padding) / (cardWidth + padding));
            int rowCount = Mathf.CeilToInt((float)allCollectibles.Count / cardsPerRow);

            Rect viewRect = new Rect(0, 0, contentRect.width - 16f, rowCount * (cardHeight + padding));

            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

            float startX = (viewRect.width - (cardsPerRow * (cardWidth + padding)) + padding) / 2f;

            for (int i = 0; i < allCollectibles.Count; i++)
            {
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;

                float x = startX + col * (cardWidth + padding);
                float y = row * (cardHeight + padding);

                Rect cardRect = new Rect(x, y, cardWidth, cardHeight);
                DrawCollectibleCard(cardRect, allCollectibles[i]);
            }

            Widgets.EndScrollView();
        }

        private void DrawCollectibleCard(Rect rect, ThingDef def)
        {
            bool isCollected = collectedDefs.Contains(def.defName);

            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.17f));
            Widgets.DrawHighlightIfMouseover(rect);

            Rect iconRect = rect.ContractedBy(8f);
            if (isCollected)
            {
                GUI.DrawTexture(iconRect, def.uiIcon);
                TooltipHandler.TipRegion(rect, $"<color=#FFD700>{def.LabelCap}</color>\n\n{def.description}");

                // [修复] Widgets.DrawBox 没有第三个参数，颜色通过 GUI.color 设置
                GUI.color = new ColorInt(212, 175, 55).ToColor; // 金色
                Widgets.DrawBox(rect, 2);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.DrawTexture(iconRect, lockedIcon);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(rect, "???");
                // [修复] Texture2D.white 不存在，使用 BaseContent.WhiteTex，但 DrawBox 默认就是白色，不需要指定
                Widgets.DrawBox(rect, 1);
            }
        }
    }
}