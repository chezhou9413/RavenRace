using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using RavenRace.Features.FusangOrganization.UI;
using RavenRace.Features.Hypnosis.Commands; // 引用
using Verse.AI;

namespace RavenRace.Features.Hypnosis
{
    [StaticConstructorOnStartup]
    public class Dialog_HypnosisControl : FusangWindowBase
    {
        private Pawn master;
        private List<Pawn> slaves;
        private Pawn selectedSlave;
        private Vector2 scrollPos;
        private Vector2 actionScrollPos; // 动作区域也可能需要滚动

        public override Vector2 InitialSize => new Vector2(800f, 550f); // 稍微加高一点
        protected override float Margin => 0f;

        public Dialog_HypnosisControl(Pawn user) : base()
        {
            this.master = user;
            this.slaves = WorldComponent_Hypnosis.Instance.GetSlaves(user);
            if (this.slaves.Count > 0) selectedSlave = slaves[0];
            this.forcePause = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            // 1. 标题
            Rect titleRect = new Rect(20, 15, inRect.width, 35);
            Text.Font = GameFont.Medium;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(titleRect, "Raven_HypnosisApp_Title".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Widgets.DrawLineHorizontal(15, 50, inRect.width - 30);

            // 2. 左侧：奴隶列表
            Rect leftPanel = new Rect(20, 60, 200, inRect.height - 80);
            DrawSlaveList(leftPanel);

            // 3. 右侧：操作面板
            Rect rightPanel = new Rect(leftPanel.xMax + 20, 60, inRect.width - leftPanel.width - 60, inRect.height - 80);
            DrawActionPanel(rightPanel);
        }

        private void DrawSlaveList(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            Rect viewRect = new Rect(0, 0, rect.width - 16, slaves.Count * 40);
            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);

            float y = 0;
            foreach (var slave in slaves)
            {
                Rect rowRect = new Rect(0, y, viewRect.width, 35);
                if (slave == selectedSlave) Widgets.DrawBoxSolid(rowRect, new Color(1f, 0.8f, 0.3f, 0.2f));

                if (Widgets.ButtonInvisible(rowRect)) selectedSlave = slave;

                Rect iconRect = new Rect(5, y + 2, 30, 30);
                Widgets.ThingIcon(iconRect, slave);

                Rect labelRect = new Rect(40, y, rowRect.width - 40, 35);
                Text.Anchor = TextAnchor.MiddleLeft;
                string status = slave.Drafted ? "[征召]" : (slave.Downed ? "[倒地]" : "");
                Widgets.Label(labelRect, $"{slave.LabelShort} {status}");
                Text.Anchor = TextAnchor.UpperLeft;
                y += 40;
            }
            Widgets.EndScrollView();
        }

        private void DrawActionPanel(Rect rect)
        {
            if (selectedSlave == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "未连接任何目标");
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // 状态显示
            Rect statusRect = new Rect(rect.x, rect.y, rect.width, 40);
            string slaveLoc = selectedSlave.Map != null ? "在线" : "位置未知";
            Widgets.Label(statusRect, $"当前连接: {selectedSlave.Name.ToStringFull} ({slaveLoc})");

            // 按钮网格 (动态生成)
            Rect gridRect = new Rect(rect.x, rect.y + 50, rect.width, rect.height - 50);

            // 获取所有指令并排序
            List<HypnosisCommandDef> commands = DefDatabase<HypnosisCommandDef>.AllDefs.OrderBy(x => x.order).ToList();

            float btnSize = 80f;
            float gap = 20f;
            int cols = (int)(gridRect.width / (btnSize + gap));
            if (cols < 1) cols = 1;

            Rect viewRect = new Rect(0, 0, gridRect.width - 16, Mathf.Ceil((float)commands.Count / cols) * (btnSize + gap + 40));

            Widgets.BeginScrollView(gridRect, ref actionScrollPos, viewRect);

            for (int i = 0; i < commands.Count; i++)
            {
                var cmd = commands[i];
                int row = i / cols;
                int col = i % cols;

                Rect btnRect = new Rect(col * (btnSize + gap), row * (btnSize + gap + 40), btnSize, btnSize);

                if (DrawActionButton(btnRect, cmd.label, cmd.Icon))
                {
                    ExecuteHypnosisCommand(selectedSlave, cmd.jobDef);
                }
                TooltipHandler.TipRegion(btnRect, cmd.description);
            }

            Widgets.EndScrollView();
        }

        private bool DrawActionButton(Rect rect, string label, Texture2D icon)
        {
            bool clicked = Widgets.ButtonImage(rect, icon ?? BaseContent.BadTex);

            Rect labelRect = new Rect(rect.x, rect.yMax + 5, rect.width, 40);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            return clicked;
        }

        private void ExecuteHypnosisCommand(Pawn slave, JobDef jobDef)
        {
            if (slave == null || slave.Dead || slave.Downed)
            {
                Messages.Message("目标无法响应指令。", MessageTypeDefOf.RejectInput);
                return;
            }

            if (slave.Drafted) slave.drafter.Drafted = false;

            Job job = JobMaker.MakeJob(jobDef);
            slave.jobs.TryTakeOrderedJob(job, JobTag.Misc);

            FleckMaker.ThrowMetaIcon(slave.Position, slave.Map, FleckDefOf.PsycastAreaEffect);
            Messages.Message($"指令已发送至 {slave.LabelShort}。", MessageTypeDefOf.TaskCompletion);
        }
    }
}