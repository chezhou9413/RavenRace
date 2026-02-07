using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace RavenRace
{
    public class Dialog_Mission_EmberSacrifice : Window
    {
        public override Vector2 InitialSize => new Vector2(950f, 650f);
        protected override float Margin => 0f; // 去除边缘缝隙

        // 消耗配置
        private const int CostIntel = 30;
        private const int CostInfluence = 50;

        private Thing radio;

        public Dialog_Mission_EmberSacrifice(Thing radio)
        {
            this.radio = radio;
            this.doCloseX = false;
            this.doCloseButton = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 绘制背景
            FusangUIStyle.DrawBackground(inRect);

            // 1. 顶部标题
            Text.Font = GameFont.Medium;
            Color oldColor = GUI.color;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(new Rect(30, 15, inRect.width, 35), "【代号：余烬】崇高奉献");
            GUI.color = oldColor;
            Text.Font = GameFont.Small;

            Widgets.DrawLineHorizontal(20, 50, inRect.width - 40);

            // 2. 内容区域背景板 (居中)
            float contentWidth = 700f;
            float contentHeight = 400f;
            Rect contentRect = new Rect((inRect.width - contentWidth) / 2f, 80f, contentWidth, contentHeight);

            Widgets.DrawBoxSolid(contentRect, new Color(0.12f, 0.12f, 0.12f));
            FusangUIStyle.DrawBorder(contentRect, FusangUIStyle.TerminalGray);

            Rect inner = contentRect.ContractedBy(20);

            // 3. 任务目标
            Rect infoRect = new Rect(inner.x, inner.y, inner.width, 30);
            GUI.color = FusangUIStyle.MainColor_Gold;
            Text.Font = GameFont.Medium;
            Widgets.Label(infoRect, "目标物资：余烬之血 x1");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Widgets.DrawLineHorizontal(inner.x, inner.y + 35, inner.width);

            // 4. 描述文本
            Rect descRect = new Rect(inner.x, inner.y + 50, inner.width, 220);
            Widgets.Label(descRect, "RavenRace_Mission_Ember_Desc".Translate());

            // 5. 消耗显示 (动态变色)
            Rect costRect = new Rect(inner.x, inner.yMax - 60, inner.width, 50);

            int curIntel = FusangResourceManager.GetAmount(FusangResourceType.Intel);
            int curInfl = FusangResourceManager.GetAmount(FusangResourceType.Influence);

            bool enoughIntel = curIntel >= CostIntel;
            bool enoughInfl = curInfl >= CostInfluence;
            bool canAfford = enoughIntel && enoughInfl;

            string intelColor = enoughIntel ? "#FFFFFF" : "#FF5555";
            string inflColor = enoughInfl ? "#FFFFFF" : "#FF5555";

            string costText = $"需求:\n" +
                              $"- 情报点数: {CostIntel}  <color={intelColor}>(当前: {curIntel})</color>\n" +
                              $"- 影响力: {CostInfluence}  <color={inflColor}>(当前: {curInfl})</color>";

            Widgets.Label(costRect, costText);

            // 6. 底部按钮区域
            float btnWidth = 160f;
            float btnHeight = 45f;
            float y = inRect.height - 70;

            // 接受按钮 (居中偏左)
            Rect acceptRect = new Rect(inRect.width / 2 - btnWidth - 20, y, btnWidth, btnHeight);

            if (FusangUIStyle.DrawButton(acceptRect, "执行协议", canAfford))
            {
                // 扣除资源并执行
                if (FusangResourceManager.TryConsume(FusangResourceType.Intel, CostIntel) &&
                    FusangResourceManager.TryConsume(FusangResourceType.Influence, CostInfluence))
                {
                    ExecuteDelivery();
                    Close();
                }
                else
                {
                    Messages.Message("资源不足，无法执行协议。", MessageTypeDefOf.RejectInput);
                }
            }

            // 返回按钮 (居中偏右)
            Rect backRect = new Rect(inRect.width / 2 + 20, y, btnWidth, btnHeight);
            if (FusangUIStyle.DrawButton(backRect, "Back".Translate()))
            {
                Close();
                Find.WindowStack.Add(new Dialog_FusangMissionBoard(radio));
            }
        }

        private void ExecuteDelivery()
        {
            Map map = radio.Map;
            if (map == null) return;

            // 生成物品
            Thing blood = ThingMaker.MakeThing(ThingDef.Named("Raven_EmberBlood"));
            blood.stackCount = 1;

            // 寻找空投点 (优先电台附近，其次交易点)
            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);

            // 尝试在电台附近 5 格内寻找可通行且无屋顶的点
            CellFinder.TryFindRandomCellNear(radio.Position, map, 5, (IntVec3 c) => c.Standable(map) && !c.Roofed(map), out IntVec3 nearRadio);
            if (nearRadio.IsValid) dropSpot = nearRadio;

            // 执行空投
            DropPodUtility.DropThingsNear(dropSpot, map, new List<Thing> { blood });

            // 提示
            Messages.Message("扶桑运输舱已抵达，余烬之血已送达。", new TargetInfo(dropSpot, map), MessageTypeDefOf.PositiveEvent);
        }
    }
}