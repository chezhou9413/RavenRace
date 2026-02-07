using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.Reproduction; // [Added]

namespace RavenRace
{
    public class Dialog_Mission_Surrogate : Window
    {
        // ... (InitialSize, CostInfluence, radio, 构造函数保持不变) ...
        public override Vector2 InitialSize => new Vector2(950f, 650f);
        protected override float Margin => 0f;
        private const int CostInfluence = 10;
        private Thing radio;

        public Dialog_Mission_Surrogate(Thing radio)
        {
            this.radio = radio;
            this.doCloseX = false;
            this.doCloseButton = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        // ... (DoWindowContents, ShowSurrogateFloatMenu 保持不变) ...
        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            // 顶部标题
            Text.Font = GameFont.Medium;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(new Rect(30, 15, inRect.width, 35), "RavenRace_Mission_Surrogate".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Widgets.DrawLineHorizontal(20, 50, inRect.width - 40);

            // 内容区域背景板
            float contentWidth = 700f;
            float contentHeight = 400f;
            Rect contentRect = new Rect((inRect.width - contentWidth) / 2f, 80f, contentWidth, contentHeight);

            Widgets.DrawBoxSolid(contentRect, new Color(0.12f, 0.12f, 0.12f));
            FusangUIStyle.DrawBorder(contentRect, FusangUIStyle.TerminalGray);

            Rect inner = contentRect.ContractedBy(20);

            // 任务代号
            Rect codeRect = new Rect(inner.x, inner.y, inner.width, 30);
            GUI.color = FusangUIStyle.MainColor_Gold;
            Text.Font = GameFont.Medium;
            Widgets.Label(codeRect, "【代号：杜鹃】");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Widgets.DrawLineHorizontal(inner.x, inner.y + 35, inner.width);

            // 描述文本
            Rect descRect = new Rect(inner.x, inner.y + 50, inner.width, 220);
            Widgets.Label(descRect, "RavenRace_Mission_Surrogate_Desc".Translate());

            // 消耗显示
            Rect costRect = new Rect(inner.x, inner.yMax - 50, inner.width, 40);
            int currentInfluence = FusangResourceManager.GetAmount(FusangResourceType.Influence);
            bool canAfford = currentInfluence >= CostInfluence;

            string costStr = $"消耗: {CostInfluence} 影响力";
            string currentStr = $"(当前: {currentInfluence})";
            string colorHex = canAfford ? "#FFFFFF" : "#FF5555";

            Text.Font = GameFont.Medium;
            Widgets.Label(costRect, $"{costStr}  <color={colorHex}>{currentStr}</color>");
            Text.Font = GameFont.Small;

            // 底部按钮区域
            float btnWidth = 160f;
            float btnHeight = 45f;
            float y = inRect.height - 70;

            // 接受按钮
            Rect acceptRect = new Rect(inRect.width / 2 - btnWidth - 20, y, btnWidth, btnHeight);

            if (FusangUIStyle.DrawButton(acceptRect, "Accept".Translate(), canAfford))
            {
                if (canAfford)
                {
                    ShowSurrogateFloatMenu();
                }
                else
                {
                    Messages.Message("影响力不足", MessageTypeDefOf.RejectInput);
                }
            }

            if (!canAfford && Mouse.IsOver(acceptRect))
            {
                TooltipHandler.TipRegion(acceptRect, "影响力不足，无法发起协议。");
            }

            // 返回按钮
            Rect backRect = new Rect(inRect.width / 2 + 20, y, btnWidth, btnHeight);
            if (FusangUIStyle.DrawButton(backRect, "Back".Translate()))
            {
                Close();
                Find.WindowStack.Add(new Dialog_FusangMissionBoard(radio));
            }
        }

        private void ShowSurrogateFloatMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (Pawn p in Find.CurrentMap.mapPawns.FreeColonists)
            {
                bool valid = p.gender == Gender.Female || RavenRaceMod.Settings.enableMalePregnancyEgg;
                if (valid && !p.Dead && !p.Downed)
                {
                    options.Add(new FloatMenuOption(p.LabelShort, () =>
                    {
                        if (FusangResourceManager.TryConsume(FusangResourceType.Influence, CostInfluence))
                        {
                            ApplySurrogacy(p);
                            Close();
                        }
                        else
                        {
                            Messages.Message("影响力不足", MessageTypeDefOf.RejectInput);
                        }
                    }));
                }
            }

            if (options.Count > 0)
                Find.WindowStack.Add(new FloatMenu(options));
            else
                Messages.Message("没有符合条件的殖民者", MessageTypeDefOf.RejectInput);
        }

        private void ApplySurrogacy(Pawn surrogate)
        {
            Faction fusang = Find.FactionManager.FirstFactionOfDef(FusangDefOf.Fusang_Hidden);
            PawnKindDef kind = PawnKindDef.Named("Raven_Colonist");
            Pawn father = PawnGenerator.GeneratePawn(kind, fusang);
            Pawn mother = PawnGenerator.GeneratePawn(kind, fusang);

            Thing egg = ThingMaker.MakeThing(RavenDefOf.Raven_SpiritEgg);
            // [Change] Comp_SpiritEgg -> CompSpiritEgg
            CompSpiritEgg eggComp = egg.TryGetComp<CompSpiritEgg>();
            if (eggComp != null)
            {
                eggComp.Initialize(mother, father, new GeneSet());
            }

            Hediff hediff = surrogate.health.AddHediff(HediffDef.Named("Raven_Hediff_SpiritEggInserted"));
            // [Change] HediffComp_SpiritEggHolder -> HediffCompSpiritEggHolder
            var holder = hediff.TryGetComp<HediffCompSpiritEggHolder>();
            if (holder != null)
            {
                holder.TryAcceptEgg(egg);
            }
            else
            {
                GenSpawn.Spawn(egg, surrogate.Position, surrogate.Map);
            }

            Messages.Message("RavenRace_Mission_Success".Translate(surrogate.LabelShort), surrogate, MessageTypeDefOf.PositiveEvent);
        }
    }
}