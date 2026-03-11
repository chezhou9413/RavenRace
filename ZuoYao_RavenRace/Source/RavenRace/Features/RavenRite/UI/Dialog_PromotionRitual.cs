using RavenRace.Features.FusangOrganization.UI;
using RavenRace.Features.RavenRite.Pojo;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RavenRace.Features.RavenRite.UI
{
    [StaticConstructorOnStartup]
    public class Dialog_PromotionRitual : FusangWindowBase
    {
        public override Vector2 InitialSize => new Vector2(1060f, 740f);
        protected override float Margin => 0f;

        //布局常量
        private const float TitleBarH = 48f;
        private const float FooterH = 64f;
        private const float PanelPad = 12f;
        private const float RowH = 62f;
        private const float AvatarSize = 46f;
        private const float SectionHeaderH = 32f;
        private const float DividerRatio = 0.50f;
        private const float RoleBtnW = 72f;   //每个角色按钮宽度
        private const float PartBtnW = 76f;   //参与者按钮宽度
        private const float BtnH = 24f;
        private static readonly Color ColRowBase = new Color(0.10f, 0.10f, 0.10f, 1f);
        private static readonly Color ColRowHover = new Color(0.16f, 0.16f, 0.16f, 1f);
        private static readonly Color ColParticipant = new Color(0.13f, 0.30f, 0.13f, 1f);
        private static readonly Color ColDisabledBtn = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        private static readonly Color ColWarning = new Color(1.00f, 0.55f, 0.15f, 1f);
        private static readonly Color ColRemoveBtn = new Color(1.00f, 0.35f, 0.35f, 1f);
        private static readonly Color ColSelectAll = new Color(0.20f, 0.55f, 0.35f, 1f);

        private static readonly Color[] RoleColors =
        {
            new Color(0.20f, 0.32f, 0.55f, 1f),
            new Color(0.42f, 0.22f, 0.55f, 1f),
            new Color(0.55f, 0.38f, 0.10f, 1f),
            new Color(0.18f, 0.45f, 0.42f, 1f),
        };

        // ── 静态纹理 ──────────────────────────────────────────────
        private static readonly Texture2D TexClose;
        private static readonly Texture2D TexBack;
        private static readonly Texture2D TexRemove;

        static Dialog_PromotionRitual()
        {
            TexClose = ContentFinder<Texture2D>.Get("UI/Widgets/CloseXSmall", true);
            TexBack = ContentFinder<Texture2D>.Get("UI/Widgets/BackArrow", false) ?? TexClose;
            TexRemove = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Leave", false)
                     ?? ContentFinder<Texture2D>.Get("UI/Buttons/Delete", false)
                     ?? BaseContent.WhiteTex;
        }
        private readonly string windowTitle;
        private readonly List<Pawn> candidatePool;
        private readonly List<RitualRoleDefinition> roleDefs;
        public Action<PromotionRitualSelection> OnConfirm;
        private readonly Dictionary<string, List<Pawn>> roleAssignments;
        private readonly HashSet<Pawn> participants = new HashSet<Pawn>();
        private Vector2 scrollLeft = Vector2.zero;
        private Vector2 scrollRight = Vector2.zero;
        public Dialog_PromotionRitual(
            string windowTitle,
            List<RitualRoleDefinition> roleDefs,
            List<Pawn> candidatePool,
            Action<PromotionRitualSelection> onConfirm = null)
            : base()
        {
            this.windowTitle = windowTitle ?? "仪式";
            this.roleDefs = roleDefs ?? new List<RitualRoleDefinition>();
            this.candidatePool = candidatePool ?? new List<Pawn>();
            this.OnConfirm = onConfirm;

            roleAssignments = new Dictionary<string, List<Pawn>>();
            foreach (var r in this.roleDefs)
                roleAssignments[r.RoleId] = new List<Pawn>();

            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }
        // 主绘制
        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);
            DrawTitleBar(new Rect(0f, 0f, inRect.width, TitleBarH));

            float contentY = TitleBarH + 8f;
            float contentH = inRect.height - contentY - FooterH - 8f;
            float leftW = inRect.width * DividerRatio - PanelPad;
            float rightW = inRect.width - leftW - PanelPad * 3f;

            DrawCandidatePanel(new Rect(PanelPad, contentY, leftW, contentH));
            DrawRolePanel(new Rect(PanelPad * 2f + leftW, contentY, rightW, contentH));
            DrawFooter(new Rect(0f, inRect.height - FooterH, inRect.width, FooterH));
        }
        // 标题栏
        private void DrawTitleBar(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            if (Widgets.ButtonImage(new Rect(12f, (rect.height - 24f) / 2f, 24f, 24f), TexBack))
                Close();

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(rect, windowTitle);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonImage(new Rect(rect.width - 38f, (rect.height - 30f) / 2f, 30f, 30f), TexClose))
                Close();
        }
        // 左侧：候选人列表
        private void DrawCandidatePanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.08f, 0.08f));
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            // 标题行 + "全选参与者"按钮
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, SectionHeaderH);
            DrawSectionHeader(headerRect, "候选人  (" + candidatePool.Count + ")");

            Rect selectAllBtn = new Rect(rect.xMax - 100f, rect.y + (SectionHeaderH - 22f) / 2f, 92f, 22f);
            Color oldColor = GUI.color;
            GUI.color = ColSelectAll;
            if (FusangUIStyle.DrawButton(selectAllBtn, "全选参与者"))
                SelectAllAsParticipants();
            GUI.color = oldColor;
            TooltipHandler.TipRegion(selectAllBtn, "将所有未分配职位的候选人设为参与者");

            Rect outer = new Rect(rect.x, rect.y + SectionHeaderH + 2f, rect.width, rect.height - SectionHeaderH - 4f);
            float viewH = candidatePool.Count * (RowH + 4f) + 4f;
            Rect viewRect = new Rect(0f, 0f, outer.width - 16f, Mathf.Max(viewH, outer.height));

            Widgets.BeginScrollView(outer.ContractedBy(4f), ref scrollLeft, viewRect);
            float y = 0f;
            foreach (var pawn in candidatePool)
            {
                DrawCandidateRow(new Rect(0f, y, viewRect.width, RowH), pawn);
                y += RowH + 4f;
            }
            Widgets.EndScrollView();
        }

        // 将所有未分配职位的 Pawn 加入参与者
        private void SelectAllAsParticipants()
        {
            foreach (var pawn in candidatePool)
            {
                if (GetAssignedRole(pawn) == null)
                    participants.Add(pawn);
            }
        }

        private void DrawCandidateRow(Rect rect, Pawn pawn)
        {
            string assignedRole = GetAssignedRole(pawn);
            bool isParticipant = participants.Contains(pawn);

            Color rowBg = assignedRole != null ? GetRoleColor(assignedRole)
                        : isParticipant ? ColParticipant
                        : Mouse.IsOver(rect) ? ColRowHover
                        : ColRowBase;

            Widgets.DrawBoxSolid(rect, rowBg);
            FusangUIStyle.DrawBorder(rect, new Color(1f, 1f, 1f, 0.06f));

            Rect inner = rect.ContractedBy(6f);
            Rect avatarRect = new Rect(inner.x, inner.y + (inner.height - AvatarSize) / 2f, AvatarSize, AvatarSize);
            Widgets.ThingIcon(avatarRect, pawn);

            // 角色名称小标签（头像左上角）
            if (assignedRole != null)
            {
                var def = roleDefs.FirstOrDefault(r => r.RoleId == assignedRole);
                if (def != null)
                {
                    Text.Font = GameFont.Tiny;
                    GUI.color = new Color(1f, 1f, 0.55f);
                    Widgets.Label(new Rect(avatarRect.x, avatarRect.y, AvatarSize, 14f), def.Label);
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
            }

            // 按钮区宽度：角色数 × RoleBtnW + 间距 + 参与者按钮
            float btnAreaW = roleDefs.Count * (RoleBtnW + 4f) + PartBtnW + 4f;
            float textX = avatarRect.xMax + 8f;
            float textW = Mathf.Max(inner.width - AvatarSize - 8f - btnAreaW, 60f);

            Text.Font = GameFont.Small;
            GUI.color = assignedRole != null ? new Color(0.75f, 0.90f, 1f) : Color.white;
            Widgets.Label(new Rect(textX, inner.y + 2f, textW, 22f), pawn.LabelShort);
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(textX, inner.y + 24f, textW, 18f), pawn.story?.TitleShort ?? "");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // 角色按钮（右侧，参与者按钮之前）
            float btnX = inner.xMax - btnAreaW;
            float btnY = inner.y + (inner.height - BtnH) / 2f;

            for (int i = 0; i < roleDefs.Count; i++)
            {
                var role = roleDefs[i];
                Rect btnRect = new Rect(btnX + i * (RoleBtnW + 4f), btnY, RoleBtnW, BtnH);
                bool inThisRole = roleAssignments[role.RoleId].Contains(pawn);
                bool roleFull = !inThisRole && role.MaxCount > 0 && roleAssignments[role.RoleId].Count >= role.MaxCount;
                bool filterBlocked = !inThisRole && !role.CanAssignPawn(pawn);
                bool blocked = !inThisRole && (isParticipant || roleFull || filterBlocked);

                if (inThisRole)
                {
                    Color old = GUI.color;
                    GUI.color = GetRoleColor(role.RoleId) * 1.4f;
                    if (FusangUIStyle.DrawButton(btnRect, "✓ " + role.Label))
                        roleAssignments[role.RoleId].Remove(pawn);
                    GUI.color = old;
                }
                else if (blocked)
                {
                    Color old = GUI.color;
                    GUI.color = ColDisabledBtn;
                    FusangUIStyle.DrawButton(btnRect, role.Label, false);
                    GUI.color = old;
                    if (Mouse.IsOver(btnRect))
                    {
                        if (filterBlocked)
                            TooltipHandler.TipRegion(btnRect, role.GetDisabledReason(pawn));
                        else if (roleFull)
                            TooltipHandler.TipRegion(btnRect, role.Label + " 已满 (" + role.MaxCount + " 人)");
                    }
                }
                else
                {
                    if (FusangUIStyle.DrawButton(btnRect, role.Label))
                    {
                        participants.Remove(pawn);
                        foreach (var kv in roleAssignments) kv.Value.Remove(pawn);
                        roleAssignments[role.RoleId].Add(pawn);
                    }
                }
            }
            // 参与者按钮
            Rect partBtn = new Rect(btnX + roleDefs.Count * (RoleBtnW + 4f), btnY, PartBtnW, BtnH);

            if (assignedRole != null)
            {
                Color old = GUI.color;
                GUI.color = ColDisabledBtn;
                FusangUIStyle.DrawButton(partBtn, "参与者", false);
                GUI.color = old;
            }
            else if (isParticipant)
            {
                if (FusangUIStyle.DrawButton(partBtn, "✓ 参与者"))
                    participants.Remove(pawn);
            }
            else
            {
                if (FusangUIStyle.DrawButton(partBtn, "参与者"))
                    participants.Add(pawn);
            }

            TooltipHandler.TipRegion(rect, () => BuildPawnTooltip(pawn), pawn.thingIDNumber);
        }

        // 右侧：角色槽 + 参与者预览

        private void DrawRolePanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.08f, 0.08f));
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            float y = rect.y;

            foreach (var role in roleDefs)
            {
                int displaySlots = role.MaxCount > 0
                    ? role.MaxCount
                    : Mathf.Max(roleAssignments[role.RoleId].Count + 1, 1);
                float blockH = SectionHeaderH + displaySlots * (RowH + 4f) + 10f;
                DrawRoleBlock(new Rect(rect.x, y, rect.width, blockH), role);
                y += blockH + 4f;
            }

            Widgets.DrawLineHorizontal(rect.x + PanelPad, y + 2f, rect.width - PanelPad * 2f);
            y += 10f;

            DrawParticipantBlock(new Rect(rect.x, y, rect.width, rect.yMax - y - 2f));
        }

        private void DrawRoleBlock(Rect rect, RitualRoleDefinition role)
        {
            Color roleColor = GetRoleColor(role.RoleId);
            var assigned = roleAssignments[role.RoleId];
            string countStr = role.MaxCount > 0
                ? assigned.Count + "/" + role.MaxCount
                : assigned.Count.ToString();

            Rect header = new Rect(rect.x, rect.y, rect.width, SectionHeaderH);
            DrawSectionHeader(header, role.Label + "  " + countStr, roleColor * 0.55f);

            // 必填标记
            if (role.Required)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = assigned.Count == 0 ? ColWarning : new Color(0.4f, 0.85f, 0.4f);
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(
                    new Rect(rect.x, rect.y, rect.width - 10f, SectionHeaderH),
                    assigned.Count == 0 ? "必填" : "✓");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            // 槽位
            float slotY = rect.y + SectionHeaderH + 4f;
            int slots = role.MaxCount > 0 ? role.MaxCount : Mathf.Max(assigned.Count + 1, 1);
            for (int i = 0; i < slots; i++)
            {
                Rect slotRect = new Rect(rect.x + PanelPad, slotY, rect.width - PanelPad * 2f, RowH);
                if (i < assigned.Count)
                    DrawFilledSlot(slotRect, assigned[i], role);
                else
                    DrawEmptySlot(slotRect, i + 1, role.Label);
                slotY += RowH + 4f;
            }
        }

        private void DrawFilledSlot(Rect rect, Pawn pawn, RitualRoleDefinition role)
        {
            Widgets.DrawBoxSolid(rect, GetRoleColor(role.RoleId));
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            Rect inner = rect.ContractedBy(6f);
            Rect avatarRect = new Rect(inner.x, inner.y + (inner.height - AvatarSize) / 2f, AvatarSize, AvatarSize);
            Widgets.ThingIcon(avatarRect, pawn);

            float textX = avatarRect.xMax + 8f;
            float textW = inner.width - AvatarSize - 8f - 32f;

            Text.Font = GameFont.Small;
            GUI.color = new Color(0.82f, 0.93f, 1f);
            Widgets.Label(new Rect(textX, inner.y + 2f, textW, 22f), pawn.LabelShort);
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(textX, inner.y + 24f, textW, 18f), pawn.story?.TitleShort ?? "");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect removeBtn = new Rect(inner.xMax - 24f, inner.y + (inner.height - 24f) / 2f, 24f, 24f);
            Color old = GUI.color;
            GUI.color = ColRemoveBtn;
            if (Widgets.ButtonImage(removeBtn, TexRemove))
                roleAssignments[role.RoleId].Remove(pawn);
            GUI.color = old;
        }

        private void DrawEmptySlot(Rect rect, int index, string roleLabel)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.11f, 0.11f, 0.11f));
            FusangUIStyle.DrawBorder(rect, new Color(1f, 1f, 1f, 0.07f));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(0.38f, 0.38f, 0.38f);
            Widgets.Label(rect, "— " + roleLabel + " · 槽位 " + index + " —");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawParticipantBlock(Rect rect)
        {
            var list = participants.ToList();
            Rect header = new Rect(rect.x, rect.y, rect.width, SectionHeaderH);
            DrawSectionHeader(header, "参与者  (" + list.Count + ")");

            Rect outer = new Rect(rect.x, rect.y + SectionHeaderH + 2f, rect.width, rect.height - SectionHeaderH - 4f);
            float viewH = list.Count * (RowH + 4f) + 4f;
            Rect viewRect = new Rect(0f, 0f, outer.width - 16f, Mathf.Max(viewH, outer.height));

            Widgets.BeginScrollView(outer.ContractedBy(4f), ref scrollRight, viewRect);

            if (list.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.38f, 0.38f, 0.38f);
                Widgets.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), "暂无参与者");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                float y = 0f;
                foreach (var pawn in list)
                {
                    Rect row = new Rect(0f, y, viewRect.width, RowH);
                    Rect inner = row.ContractedBy(6f);
                    Rect avatar = new Rect(inner.x, inner.y + (inner.height - AvatarSize) / 2f, AvatarSize, AvatarSize);

                    Widgets.DrawBoxSolid(row, ColParticipant);
                    FusangUIStyle.DrawBorder(row, new Color(1f, 1f, 1f, 0.06f));
                    Widgets.ThingIcon(avatar, pawn);

                    float textX = avatar.xMax + 8f;
                    float textW = inner.width - AvatarSize - 8f - 32f;

                    Text.Font = GameFont.Small;
                    GUI.color = Color.white;
                    Widgets.Label(new Rect(textX, inner.y + 2f, textW, 22f), pawn.LabelShort);
                    GUI.color = Color.gray;
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(new Rect(textX, inner.y + 24f, textW, 18f), pawn.story?.TitleShort ?? "");
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;

                    Rect removeBtn = new Rect(inner.xMax - 24f, inner.y + (inner.height - 24f) / 2f, 24f, 24f);
                    Color old = GUI.color;
                    GUI.color = ColRemoveBtn;
                    if (Widgets.ButtonImage(removeBtn, TexRemove))
                        participants.Remove(pawn);
                    GUI.color = old;

                    y += RowH + 4f;
                }
            }

            Widgets.EndScrollView();
        }
        // 底部操作栏
        private void DrawFooter(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            var blockReasons = new List<string>();
            foreach (var role in roleDefs)
            {
                if (role.Required && roleAssignments[role.RoleId].Count == 0)
                    blockReasons.Add("需要指定「" + role.Label + "」");
            }

            bool canConfirm = blockReasons.Count == 0;
            int roleCount = roleAssignments.Values.Sum(l => l.Count);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = canConfirm ? Color.gray : ColWarning;

            string hint = canConfirm
                ? "就绪 — 角色 " + roleCount + " 人，参与者 " + participants.Count + " 人"
                : string.Join("  |  ", blockReasons);

            Widgets.Label(new Rect(PanelPad * 2f, rect.y, rect.width * 0.60f, rect.height), hint);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // 按钮区：取消 + 开始仪式，留足间距防截断
            float btnY = rect.y + (rect.height - 36f) / 2f;
            if (FusangUIStyle.DrawButton(new Rect(rect.xMax - 228f, btnY, 100f, 36f), "取消"))
                Close();

            if (FusangUIStyle.DrawButton(new Rect(rect.xMax - 120f, btnY, 112f, 36f), "开始仪式", canConfirm))
            {
                OnConfirm?.Invoke(new PromotionRitualSelection
                {
                    RoleAssignments = roleAssignments.ToDictionary(
                        kv => kv.Key,
                        kv => new List<Pawn>(kv.Value)),
                    Participants = participants.ToList()
                });
                Close();
            }
        }
        // 辅助方法
        private string GetAssignedRole(Pawn pawn)
        {
            foreach (var kv in roleAssignments)
                if (kv.Value.Contains(pawn)) return kv.Key;
            return null;
        }

        private Color GetRoleColor(string roleId)
        {
            int idx = roleDefs.FindIndex(r => r.RoleId == roleId);
            if (idx < 0) return RoleColors[0];
            Color? custom = roleDefs[idx].SlotColor;
            return custom.HasValue ? custom.Value : RoleColors[idx % RoleColors.Length];
        }

        private static void DrawSectionHeader(Rect rect, string label, Color? bgOverride = null)
        {
            Color bg = bgOverride.HasValue ? bgOverride.Value : new Color(1f, 1f, 1f, 0.05f);
            Widgets.DrawBoxSolid(rect, bg);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(new Rect(rect.x + 10f, rect.y, rect.width - 20f, rect.height), label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static string BuildPawnTooltip(Pawn pawn)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(pawn.LabelCap);
            if (pawn.story?.TitleShort != null) sb.AppendLine(pawn.story.TitleShort);
            if (pawn.skills != null)
            {
                sb.AppendLine();
                foreach (var s in pawn.skills.skills.OrderByDescending(x => x.Level).Take(3))
                    sb.AppendLine(s.def.LabelCap + ": " + s.Level);
            }
            return sb.ToString().TrimEnd();
        }
    }
}