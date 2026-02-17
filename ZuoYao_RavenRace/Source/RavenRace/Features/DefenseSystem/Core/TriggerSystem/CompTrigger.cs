using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;

namespace RavenRace
{
    public class CompProperties_Trigger : CompProperties
    {
        public TriggerCondition condition = TriggerCondition.StepOn;
        public float detectionRadius = 0f;
        public float viewAngle = 360f;
        public bool manualTriggerable = true;
        public int cooldownTicks = -1;

        // 触发所需的敌对单位数量
        public int hostileCountThreshold = 1;

        // [新增] 严格的种族限制字段
        public bool restrictToAnimals = false;  // 仅限动物 (非人、非机械)
        public bool restrictToWild = false;     // 仅限野生 (无派系)
        public bool requireFuelToTrigger = false; // 必须有燃料才能触发


        public CompProperties_Trigger()
        {
            this.compClass = typeof(CompTrigger);
        }
    }

    public class CompTrigger : ThingComp
    {
        public CompProperties_Trigger Props => (CompProperties_Trigger)this.props;

        private int cooldownTicksLeft = 0;
        private bool isArmed = true;
        private Thing attachedWall;

        // [Fixed] 用于记录上一帧墙体的血量，防止瞬爆
        private int lastWallHitPoints = -1;

        // [新增] 缓存燃料组件
        private CompRefuelable refuelableComp;

        public bool IsArmed
        {
            get
            {
                // 如果需要燃料且没燃料，视为未武装
                if (Props.requireFuelToTrigger && refuelableComp != null && !refuelableComp.HasFuel)
                {
                    return false;
                }
                return isArmed;
            }
        }

        public bool OnCooldown => cooldownTicksLeft > 0;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref cooldownTicksLeft, "cooldownTicksLeft", 0);
            Scribe_Values.Look<bool>(ref isArmed, "isArmed", true);
            // 不需要保存 lastWallHitPoints，加载后重新获取即可
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // 缓存组件
            refuelableComp = parent.GetComp<CompRefuelable>();

            if (Props.condition == TriggerCondition.WallDamage)
            {
                IntVec3 wallPos = parent.Position - parent.Rotation.FacingCell;
                attachedWall = wallPos.GetEdifice(parent.Map);

                // [Fixed] 初始化时记录当前血量，而不是用 MaxHitPoints 判断
                if (attachedWall != null)
                {
                    lastWallHitPoints = attachedWall.HitPoints;
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            // 冷却逻辑
            if (cooldownTicksLeft > 0)
            {
                cooldownTicksLeft--;
                // 冷却结束自动重置
                if (cooldownTicksLeft <= 0)
                {
                    Rearm();
                }
            }

            // [核心修改] 燃料检查：如果没燃料，直接跳过所有检测
            if (!IsArmed) return;
            if (OnCooldown) return;

            // ... (接近触发逻辑) ...
            if (Props.detectionRadius > 0 && this.parent.IsHashIntervalTick(10))
            {
                CheckProximity();
            }

            // ... (墙体受损逻辑) ...
            if (Props.condition == TriggerCondition.WallDamage)
            {
                // (保持原逻辑不变)
                if (attachedWall == null || attachedWall.Destroyed)
                {
                    TryTrigger(null);
                }
                else
                {
                    if (attachedWall.HitPoints < lastWallHitPoints) TryTrigger(null);
                    lastWallHitPoints = attachedWall.HitPoints;
                }
            }
        }



        public void Notify_SteppedOn(Pawn p)
        {
            if (Props.condition == TriggerCondition.StepOn)
            {
                TryTrigger(p);
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(ref dinfo, out absorbed);
            if (absorbed) return;

            if (Props.condition == TriggerCondition.Damage && isArmed)
            {
                TryTrigger(dinfo.Instigator as Pawn);
            }
        }

        private void CheckProximity()
        {
            if (Props.detectionRadius <= 0) return;

            var candidates = GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, Props.detectionRadius, true);

            int validHostiles = 0;
            Pawn lastValidPawn = null;

            foreach (Thing t in candidates)
            {
                if (t is Pawn p && IsValidTarget(p))
                {
                    // 角度检查
                    if (Props.viewAngle < 360f)
                    {
                        float angleToPawn = (p.Position - parent.Position).AngleFlat;
                        float myAngle = parent.Rotation.AsAngle;
                        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(myAngle, angleToPawn));

                        if (angleDiff > Props.viewAngle / 2f) continue;
                    }

                    validHostiles++;
                    lastValidPawn = p;
                }
            }

            // 检查数量阈值
            if (validHostiles >= Props.hostileCountThreshold)
            {
                TryTrigger(lastValidPawn);
            }
        }

        // [重点] 核心判定逻辑
        private bool IsValidTarget(Pawn p)
        {
            if (p == null || p.Dead) return false;

            // 1. 严格的种族限制 (捕兽夹专用)
            if (Props.restrictToAnimals)
            {
                // 必须是动物 (排除人类、机械族、甚至排除异象实体)
                if (!p.RaceProps.Animal) return false;
            }

            // 2. 严格的野性限制 (捕兽夹专用)
            if (Props.restrictToWild)
            {
                // A. 必须无派系 (野生)
                if (p.Faction != null) return false;

                // B. [新增] 必须未标记驯服 (DesignationManager)
                if (parent.Map != null)
                {
                    if (parent.Map.designationManager.DesignationOn(p, DesignationDefOf.Tame) != null) return false;
                }
            }

            // 3. 友军安全 (通用)
            // 注意：如果是捕兽夹模式，上面的限制已经足够强了，但为了保险还是保留
            if (RavenRaceMod.Settings.friendlyFireSafe && !Props.restrictToWild)
            {
                if (p.Faction == Faction.OfPlayer || !p.HostileTo(Faction.OfPlayer))
                    return false;
            }

            return true;
        }

        public void TryTrigger(Pawn triggerer)
        {
            // 双重检查 IsArmed (含燃料检查)
            if (!IsArmed || OnCooldown) return;
            if (triggerer != null && !IsValidTarget(triggerer)) return;

            var effects = this.parent.GetComps<CompTrapEffect>();
            foreach (var effect in effects)
            {
                effect.OnTriggered(triggerer);
            }

            // 冷却处理
            if (Props.cooldownTicks < 0)
            {
                isArmed = false; // 永久失效 (一次性陷阱)
            }
            else
            {
                cooldownTicksLeft = Props.cooldownTicks; // 进入冷却，之后会自动 Rearm
            }
        }

        public void Rearm()
        {
            cooldownTicksLeft = 0;
            isArmed = true;

            // 重置墙体血量记录
            if (attachedWall != null)
            {
                lastWallHitPoints = attachedWall.HitPoints;
            }

            var effects = this.parent.GetComps<CompTrapEffect>();
            foreach (var effect in effects)
            {
                effect.OnRearm();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (RavenRaceMod.Settings.enableDebugMode || RavenRaceMod.Settings.enableDefenseSystemDebug)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Trigger",
                    action = () => TryTrigger(null)
                };
            }

            if (Props.manualTriggerable && isArmed)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Trigger Manually",
                    icon = TexCommand.Attack,
                    action = () => TryTrigger(null)
                };
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            if ((RavenRaceMod.Settings.enableDefenseSystemDebug || Props.detectionRadius > 0) && Props.detectionRadius > 0)
            {
                if (Props.viewAngle >= 360f)
                {
                    GenDraw.DrawRadiusRing(parent.Position, Props.detectionRadius);
                }
                else
                {
                    float range = Props.detectionRadius;
                    float angle = parent.Rotation.AsAngle;
                    float halfAngle = Props.viewAngle / 2f;

                    List<IntVec3> cells = new List<IntVec3>();
                    foreach (var cell in GenRadial.RadialCellsAround(parent.Position, range, true))
                    {
                        if (cell == parent.Position) continue;
                        if (!cell.InBounds(parent.Map)) continue;

                        float cellAngle = (cell - parent.Position).AngleFlat;
                        if (Mathf.Abs(Mathf.DeltaAngle(angle, cellAngle)) <= halfAngle)
                        {
                            cells.Add(cell);
                        }
                    }

                    if (cells.Count > 0)
                    {
                        GenDraw.DrawFieldEdges(cells, Color.white);
                    }
                }
            }
        }
    }
}