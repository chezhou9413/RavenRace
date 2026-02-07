using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using RavenRace.Features.Reproduction; // [Added]

namespace RavenRace
{
    public class Dialog_SoulAltar : Window
    {
        private CompSoulAltar altar;
        public override Vector2 InitialSize => new Vector2(950, 750);

        private const float GridSize = 40f;
        private const float GridGap = 4f;

        public Dialog_SoulAltar(CompSoulAltar altar)
        {
            this.altar = altar;
            this.doCloseX = true;
            this.doCloseButton = true;

            this.forcePause = false;
            this.absorbInputAroundWindow = false;
            this.closeOnClickedOutside = true;
            this.preventCameraMotion = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            altar.ScanNetwork();

            Widgets.DrawBoxSolid(inRect, new Color(0.05f, 0.05f, 0.05f));
            DrawGrid(inRect);

            Rect headerRect = new Rect(20, 20, 400, 150);
            Text.Font = GameFont.Medium;
            GUI.color = new Color(1f, 0.8f, 0.2f);
            Widgets.Label(new Rect(headerRect.x, headerRect.y, headerRect.width, 35), "扶桑育生祭坛 - 阵列监控");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            int activePylons = altar.connectedPylons.Count(kvp => kvp.Value.GetComp<CompPowerTrader>()?.PowerOn ?? false);
            float speedMult = altar.GetSpeedMultiplier();

            string status = $"孵化速度: {speedMult:P0}\n" +
                            $"----------------\n" +
                            $"有效共鸣桩: {activePylons} / 12 (每个 +{altar.Props.hatchingSpeedPerPylon:P0})\n" +
                            $"有效注灵器: {altar.connectedInfusers.Count} / 8\n" +
                            $"有效注入仪: {altar.connectedInjectors.Count} / 4";

            Widgets.Label(new Rect(20, 60, 300, 120), status);

            Rect leftPanel = new Rect(20, 180, 300, inRect.height - 200);
            DrawLeftPanel(leftPanel);

            DrawAltarLayout(inRect);
        }

        private void DrawLeftPanel(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            Building_Cradle cradle = altar.parent as Building_Cradle;
            // [Change] Comp_SpiritEgg -> CompSpiritEgg
            CompSpiritEgg eggComp = null;
            if (cradle != null && cradle.GetDirectlyHeldThings().Count > 0)
                eggComp = cradle.GetDirectlyHeldThings()[0].TryGetComp<CompSpiritEgg>();

            if (eggComp == null)
            {
                GUI.color = Color.gray;
                listing.Label("状态: [空置] 等待灵卵");
            }
            else if (eggComp.isIncubating)
            {
                GUI.color = Color.cyan;
                listing.Label($"状态: [孵化中] {eggComp.Progress:P1}");

                float totalNeeded = eggComp.TotalTicksNeeded;
                float currentSpeed = altar.GetSpeedMultiplier();
                float remainingTicks = (totalNeeded * (1f - eggComp.Progress)) / currentSpeed;

                listing.Label($"剩余时间: {((int)remainingTicks).ToStringTicksToPeriod()}");
            }
            else
            {
                GUI.color = Color.yellow;
                listing.Label("状态: [就绪] 等待启动");
            }
            GUI.color = Color.white;
            listing.GapLine();

            if (eggComp == null || !eggComp.isIncubating)
            {
                DrawRequirements(listing);
                listing.GapLine();
            }

            listing.Label("当前强化预览:");

            if (eggComp != null && eggComp.isIncubating)
            {
                listing.Label("(仪式进行中 - 强化已固化)");
                if (eggComp.storedUpgradeDefNames != null)
                {
                    var lockedDefs = new List<SoulAltarUpgradeDef>();
                    foreach (var name in eggComp.storedUpgradeDefNames)
                    {
                        var d = DefDatabase<SoulAltarUpgradeDef>.GetNamedSilentFail(name);
                        if (d != null) lockedDefs.Add(d);
                    }
                    DrawUpgradeList(listing, lockedDefs);
                }
            }
            else
            {
                var potentialUpgrades = altar.GetPotentialUpgrades();
                if (potentialUpgrades.Count == 0)
                {
                    GUI.color = Color.gray;
                    listing.Label("- 无有效注灵 -");
                    GUI.color = Color.white;
                }
                else
                {
                    DrawUpgradeList(listing, potentialUpgrades);
                }
            }

            listing.End();
        }

        // ... (其余方法 DrawRequirements, DrawUpgradeList, DrawGrid, DrawAltarLayout, DrawComponents 等保持不变) ...

        private void DrawRequirements(Listing_Standard listing)
        {
            Dictionary<ThingDef, int> reqs = new Dictionary<ThingDef, int>();

            void CountReq(Building_AltarInfuser infuser)
            {
                if (infuser.targetDef != null && infuser.innerContainer.Count == 0)
                {
                    if (reqs.ContainsKey(infuser.targetDef)) reqs[infuser.targetDef]++;
                    else reqs[infuser.targetDef] = 1;
                }
            }

            foreach (var kvp in altar.connectedInfusers) CountReq(kvp.Value);
            foreach (var kvp in altar.connectedInjectors) CountReq(kvp.Value);

            if (reqs.Count > 0)
            {
                listing.Label("待搬运物资:");
                foreach (var kvp in reqs)
                {
                    GUI.color = new Color(1f, 0.8f, 0.4f);
                    listing.Label($"- {kvp.Key.LabelCap} x{kvp.Value}");
                }
                GUI.color = Color.white;
            }
            else
            {
                listing.Label("所有指定物资已就绪。");
            }
        }

        private void DrawUpgradeList(Listing_Standard listing, List<SoulAltarUpgradeDef> upgrades)
        {
            var grouped = upgrades.GroupBy(u => u).ToDictionary(g => g.Key, g => g.Count());

            foreach (var kvp in grouped)
            {
                var up = kvp.Key;
                int count = kvp.Value;
                string suffix = count > 1 ? $" x{count}" : "";

                GUI.color = new Color(0.7f, 1f, 0.7f);
                listing.Label($"> {up.label}{suffix}");
                GUI.color = Color.white;

                if (up.skillGains != null)
                    foreach (var sk in up.skillGains)
                        listing.Label($"  [技能] {sk.skill.label} +{sk.xp * count} xp");

                if (up.statOffsets != null)
                    foreach (var st in up.statOffsets)
                        listing.Label($"  [属性] {st.stat.label} {(st.value * count).ToStringByStyle(st.stat.toStringStyle)}");

                if (up.forcedTraits != null)
                    foreach (var tr in up.forcedTraits)
                        listing.Label($"  [特性] {tr.degreeDatas[0].label}");
            }
        }

        private void DrawGrid(Rect rect)
        {
            float step = GridSize + GridGap;
            Vector2 center = new Vector2(rect.width / 2 + 100, rect.height / 2);
            Color cellColor = new Color(1f, 1f, 1f, 0.03f);
            int range = 12;
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    float drawX = center.x + x * step - GridSize / 2;
                    float drawY = center.y + y * step - GridSize / 2;
                    Rect cellRect = new Rect(drawX, drawY, GridSize, GridSize);
                    Widgets.DrawBoxSolid(cellRect, cellColor);
                }
            }
        }

        private void DrawAltarLayout(Rect rect)
        {
            Vector2 screenCenter = new Vector2(rect.width / 2 + 100, rect.height / 2);

            Building_Cradle cradle = altar.parent as Building_Cradle;
            bool hasEgg = cradle != null && cradle.GetDirectlyHeldThings().Count > 0;

            bool isIncubating = false;
            float progress = 0f;
            if (hasEgg)
            {
                // [Change] Comp_SpiritEgg -> CompSpiritEgg
                var egg = cradle.GetDirectlyHeldThings()[0].TryGetComp<CompSpiritEgg>();
                if (egg != null)
                {
                    isIncubating = egg.isIncubating;
                    progress = egg.Progress;
                }
            }

            Rect coreRect = DrawBuildingCell(screenCenter, 0, 0, 3, 3, new Color(0.8f, 0.8f, 0.8f), null, true);

            if (hasEgg)
            {
                if (isIncubating)
                {
                    Rect barRect = coreRect.ContractedBy(15);
                    Widgets.FillableBar(barRect, progress);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(barRect, $"孵化中\n{progress:P0}");
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                else
                {
                    Rect btnRect = coreRect.ContractedBy(20);
                    if (Widgets.ButtonText(btnRect, "启动仪式"))
                    {
                        altar.TryStartIncubation();
                    }
                }
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(coreRect, "摇篮空置");
                Text.Anchor = TextAnchor.UpperLeft;
            }

            DrawComponents(screenCenter, isIncubating);
        }

        private void DrawComponents(Vector2 center, bool isLocked)
        {
            foreach (var offset in AltarGeometryUtility.InfuserOffsets)
                DrawComponentNode(center, offset, "Raven_Altar_Infuser", "注灵", altar.connectedInfusers.ContainsKey(offset), false, isLocked);

            foreach (var offset in AltarGeometryUtility.PylonOffsets)
                DrawComponentNode(center, offset, "Raven_Altar_Pylon", "桩", altar.connectedPylons.ContainsKey(offset), true, isLocked);

            foreach (var offset in AltarGeometryUtility.InjectorOffsets)
                DrawComponentNode(center, offset, "Raven_Altar_Injector", "蜕凡", altar.connectedInjectors.ContainsKey(offset), false, isLocked);
        }

        private void DrawComponentNode(Vector2 center, IntVec3 offset, string defName, string label, bool connected, bool isPylon = false, bool isLocked = false)
        {
            float step = GridSize + GridGap;
            Vector2 pos = center + new Vector2(offset.x * step, -offset.z * step);

            Color linkColor = connected ? new Color(0f, 1f, 0f, 0.15f) : new Color(0.3f, 0.3f, 0.3f, 0.1f);
            if (connected) DrawLine(center, pos, linkColor);

            Color cellColor = new Color(0.2f, 0.2f, 0.2f);
            Building_AltarInfuser infuser = null;

            if (connected)
            {
                if (isPylon)
                {
                    var pylon = altar.connectedPylons[offset];
                    bool powered = pylon.GetComp<CompPowerTrader>()?.PowerOn ?? false;
                    cellColor = powered ? Color.cyan : Color.red;
                }
                else
                {
                    infuser = defName == "Raven_Altar_Injector" ? altar.connectedInjectors[offset] : altar.connectedInfusers[offset];

                    bool hasItem = infuser.GetDirectlyHeldThings().Count > 0;
                    bool hasTarget = infuser.targetDef != null;

                    if (hasItem) cellColor = Color.green;
                    else if (hasTarget) cellColor = new Color(1f, 0.8f, 0f);
                    else cellColor = new Color(0.4f, 0.4f, 0.4f);
                }
            }

            Rect nodeRect = DrawBuildingCell(pos, 0, 0, 1, 1, cellColor, null, connected);

            if (infuser != null)
            {
                Texture2D icon = infuser.GetUIIcon();
                if (icon != null)
                {
                    GUI.color = (infuser.GetDirectlyHeldThings().Count == 0) ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
                    Widgets.DrawTextureFitted(nodeRect.ContractedBy(4), icon, 1f);
                    GUI.color = Color.white;
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(nodeRect, label);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }

                if (!isLocked && connected && Widgets.ButtonInvisible(nodeRect))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    AltarComponentType type = (defName == "Raven_Altar_Injector") ? AltarComponentType.Injector : AltarComponentType.Infuser;
                    var upgrades = DefDatabase<SoulAltarUpgradeDef>.AllDefsListForReading.Where(u => u.slotType == type);

                    foreach (var up in upgrades)
                    {
                        string menuLabel = $"{up.label} (需: {up.inputItem.LabelCap})";
                        options.Add(new FloatMenuOption(menuLabel, () => infuser.SetTarget(up.inputItem), up.inputItem));
                    }

                    if (infuser.targetDef != null)
                    {
                        options.Add(new FloatMenuOption("清除指派", () => infuser.SetTarget(null)));
                    }

                    if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                }

                if (Mouse.IsOver(nodeRect))
                {
                    string tip = "";
                    if (isLocked) tip = "仪式进行中 - 已锁定";
                    else if (infuser.innerContainer.Count > 0) tip = $"已放入: {infuser.innerContainer[0].Label}";
                    else if (infuser.targetDef != null) tip = $"等待搬运: {infuser.targetDef.LabelCap}";
                    else tip = "点击选择注灵材料";
                    TooltipHandler.TipRegion(nodeRect, tip);
                }
            }
        }

        private Rect DrawBuildingCell(Vector2 pos, int offsetX, int offsetZ, int width, int height, Color color, string label, bool active)
        {
            float w = width * GridSize + (width - 1) * GridGap;
            float h = height * GridSize + (height - 1) * GridGap;
            Rect r = new Rect(pos.x - w / 2, pos.y - h / 2, w, h);

            GUI.color = new Color(color.r, color.g, color.b, 0.3f);
            Widgets.DrawBoxSolid(r, GUI.color);
            GUI.color = color;
            Widgets.DrawBox(r, 1);

            return r;
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            Widgets.DrawLine(start, end, color, 2f);
            GUI.color = old;
        }
    }
}