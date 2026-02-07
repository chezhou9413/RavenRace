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

        public bool IsArmed => isArmed;
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

            if (cooldownTicksLeft > 0)
            {
                cooldownTicksLeft--;
                if (cooldownTicksLeft <= 0)
                {
                    Rearm();
                }
            }

            if (!isArmed || OnCooldown) return;

            // 1. 接近触发 (支持数量阈值)
            if (Props.detectionRadius > 0 && this.parent.IsHashIntervalTick(10))
            {
                CheckProximity();
            }

            // 2. 墙体受损检测 [Fixed Logic]
            if (Props.condition == TriggerCondition.WallDamage)
            {
                // 如果墙没了，或者血量比上一帧记录的低，说明受损了
                if (attachedWall == null || attachedWall.Destroyed)
                {
                    // 墙没了，触发
                    TryTrigger(null);
                }
                else
                {
                    // 仅当当前血量低于记录值时触发（即受到了新的伤害）
                    // 忽略稍微的浮点误差，HitPoints是int所以直接比较
                    if (attachedWall.HitPoints < lastWallHitPoints)
                    {
                        TryTrigger(null);
                    }

                    // 更新记录值
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

        private bool IsValidTarget(Pawn p)
        {
            if (p == null || p.Dead) return false;

            if (RavenRaceMod.Settings.friendlyFireSafe)
            {
                if (p.Faction == Faction.OfPlayer || !p.HostileTo(Faction.OfPlayer))
                    return false;
            }

            return true;
        }

        public void TryTrigger(Pawn triggerer)
        {
            if (!isArmed || OnCooldown) return;
            if (triggerer != null && !IsValidTarget(triggerer)) return;

            var effects = this.parent.GetComps<CompTrapEffect>();
            foreach (var effect in effects)
            {
                effect.OnTriggered(triggerer);
            }

            if (Props.cooldownTicks < 0)
            {
                isArmed = false;
            }
            else
            {
                cooldownTicksLeft = Props.cooldownTicks;
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