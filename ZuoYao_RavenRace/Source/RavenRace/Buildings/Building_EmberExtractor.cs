using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;
using Verse.AI;
using System.Text;

namespace RavenRace.Buildings
{
    /// <summary>
    /// 提取仪专用属性类，用于加载自定义 XML 贴图节点。
    /// </summary>
    public class BuildingProperties_EmberExtractor : BuildingProperties
    {
        public GraphicData emberExtractorTankGraphic = null;
    }

    /// <summary>
    /// 残火提取仪：实现人员进入、多层贴图渲染及属性逻辑。
    /// [1.6 规范] 必须使用 StaticConstructorOnStartup 以确保 Texture2D 在主线程初始化。
    /// </summary>
    [StaticConstructorOnStartup]
    public class Building_EmberExtractor : Building_Enterable, IThingHolderWithDrawnPawn
    {
        // ==========================================
        // 字段
        // ==========================================
        private const int ExtractionDuration = 60000;
        private int ticksRemaining = ExtractionDuration;
        private CompPowerTrader powerComp;
        private Graphic tankGraphic;

        // [1.6 修复] 使用绝对安全的原版路径，并在静态构造函数中显式加载
        private static readonly Texture2D InsertIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);
        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);

        // ==========================================
        // 属性
        // ==========================================
        public Pawn ContainedPawn => innerContainer.FirstOrDefault() as Pawn;
        public bool PowerOn => powerComp != null && powerComp.PowerOn;
        private BuildingProperties_EmberExtractor BuildingProps => (BuildingProperties_EmberExtractor)def.building;

        // ==========================================
        // 渲染接口 (IThingHolderWithDrawnPawn)
        // ==========================================
        public float HeldPawnDrawPos_Y => DrawPos.y + Altitudes.AltInc;
        public float HeldPawnBodyAngle => Rotation.AsAngle;
        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;
        public override Vector3 PawnDrawOffset => Vector3.zero;

        // ==========================================
        // 生命周期
        // ==========================================
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", ExtractionDuration);
        }

        protected override void Tick()
        {
            base.Tick();
            if (Working && ContainedPawn != null && PowerOn)
            {
                ticksRemaining--;
                if (ticksRemaining <= 0) FinishExtraction();
            }
        }

        // ==========================================
        // 渲染核心 (DrawAt)
        // ==========================================
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // 1. 绘制底座
            base.DrawAt(drawLoc, flip);

            // 2. 绘制内部 Pawn
            if (Working && ContainedPawn != null)
            {
                Vector3 pawnDrawPos = this.DrawPos;
                pawnDrawPos.z += 0.52f; // 微调 Z 轴位置使小人居中

                // [1.6 规范] 调用 RenderPawnAt 并在渲染后确保节点状态
                ContainedPawn.Drawer.renderer.RenderPawnAt(pawnDrawPos, null, false);
            }

            // 3. 绘制覆盖罐体
            if (this.tankGraphic == null && BuildingProps.emberExtractorTankGraphic != null)
            {
                this.tankGraphic = BuildingProps.emberExtractorTankGraphic.GraphicColoredFor(this);
            }

            if (this.tankGraphic != null)
            {
                Vector3 tankPos = drawLoc;
                tankPos.y += Altitudes.AltInc * 2; // Y轴抬高，确保覆盖在底座和小人之上
                this.tankGraphic.Draw(tankPos, Rotation, this, 0f);
            }
        }

        // ==========================================
        // 逻辑处理
        // ==========================================
        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (!pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony) return false;
            if (pawn.def.defName != "Raven_Race") return "RavenRace_Extractor_OnlyRaven".Translate();
            if (pawn.health.hediffSet.HasHediff(RavenDefOf.Raven_Hediff_EmberDrain))
                return "RavenRace_Extractor_InCooldown".Translate(pawn.Named("PAWN"));
            if (!PowerOn) return "NoPower".Translate().CapitalizeFirst();
            if (innerContainer.Count > 0) return "Occupied".Translate();
            return true;
        }

        public override void TryAcceptPawn(Pawn pawn)
        {
            if (!CanAcceptPawn(pawn)) return;
            pawn.DeSpawnOrDeselect(DestroyMode.Vanish);
            if (innerContainer.TryAddOrTransfer(pawn, true))
            {
                selectedPawn = pawn;
                startTick = Find.TickManager.TicksGame;
                ticksRemaining = ExtractionDuration;
                // [核心] 改变状态后标记渲染为脏
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        private void FinishExtraction()
        {
            Pawn pawn = ContainedPawn;
            if (pawn == null) { ResetState(); return; }

            Thing blood = ThingMaker.MakeThing(ThingDef.Named("Raven_EmberBlood"));
            GenDrop.TryDropSpawn(blood, InteractionCell, Map, ThingPlaceMode.Near, out Thing _);

            pawn.health.AddHediff(RavenDefOf.Raven_Hediff_EmberDrain);
            if (!pawn.health.hediffSet.HasHediff(HediffDefOf.Anesthetic))
                pawn.health.AddHediff(HediffDefOf.Anesthetic);

            innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);

            // [核心修复] 弹出人员后必须彻底刷新其渲染树
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            ResetState();

            Messages.Message("RavenRace_Extractor_Finished_Safe".Translate(pawn.LabelShort), new LookTargets(this, blood, pawn), MessageTypeDefOf.PositiveEvent);
        }

        private void ResetState() { selectedPawn = null; startTick = -1; ticksRemaining = ExtractionDuration; }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) yield return gizmo;
            if (Working)
            {
                yield return new Command_Action { defaultLabel = "CommandCancel".Translate(), icon = CancelIcon, action = CancelExtraction };
            }
            else if (innerContainer.Count == 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RavenRace_InsertPerson".Translate() + "...",
                    icon = InsertIcon,
                    action = OpenInsertFloatMenu
                };
            }
        }

        private void OpenInsertFloatMenu()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (Pawn p in Map.mapPawns.AllPawnsSpawned)
            {
                if (p.IsColonist || p.IsPrisonerOfColony || p.IsSlaveOfColony)
                {
                    AcceptanceReport ar = CanAcceptPawn(p);
                    if (ar.Accepted) list.Add(new FloatMenuOption(p.LabelShortCap, () => SelectPawn(p)));
                    else list.Add(new FloatMenuOption(p.LabelShortCap + ": " + ar.Reason, null));
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        private void CancelExtraction() { innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near); ResetState(); }
    }
}