using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Ayaya
{
    /// <summary>
    /// 无双风神 - 自定义跳跃Verb
    /// 核心修复：RimWorld 在某些 UI 预测帧中生成的 Verb 实例不会自动绑定 ability 字段，
    /// 这会导致 Targeter.ConfirmStillValid() 中的 null 引用。
    /// 我们通过主动抓取 CasterPawn 上的对应 Ability 来确保 ability 永远不为空。
    /// </summary>
    public class Verb_AyayaMusouFuujin : Verb_CastAbilityJump
    {
        private const float PathDamage = 20f;
        private const float PathArmorPen = 0.6f;
        private const float DanmakuRadius = 8f;

        public override ThingDef JumpFlyerDef => AyayaDefOf.Raven_PawnFlyer_AyayaDash;

        /// <summary>
        /// 【救命稻草】：确保 ability 字段被正确填充。
        /// 这是为了对抗原版 Targeter 在每帧对 verb_CastAbility.ability.CanQueueCast 的强行读取。
        /// 我们在每次需要用到 ability 之前，主动检测并自我修复。
        /// </summary>
        private void EnsureAbilityBound()
        {
            if (this.ability == null && this.caster is Pawn p && p.abilities != null)
            {
                // 主动从 Pawn 的能力列表中抓取这个技能绑定给自己
                this.ability = p.abilities.GetAbility(AyayaDefOf.Raven_Ability_Ayaya_MusouFuujin);
            }
        }

        protected override bool TryCastShot()
        {
            EnsureAbilityBound();
            if (this.ability == null) return false;

            Pawn caster = this.CasterPawn;
            if (caster == null || !caster.Spawned) return false;

            IntVec3 startCell = caster.Position;
            IntVec3 destCell = this.currentTarget.Cell;
            Map map = caster.Map;
            if (map == null) return false;

            List<IntVec3> pathCells = GenSight.BresenhamCellsBetween(startCell, destCell);
            HashSet<int> hitPawnIds = new HashSet<int>();
            for (int i = 0; i < pathCells.Count; i++)
            {
                IntVec3 cell = pathCells[i];
                if (!cell.InBounds(map)) continue;
                List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
                for (int j = things.Count - 1; j >= 0; j--)
                {
                    if (things[j] is Pawn victim && victim != caster && !hitPawnIds.Contains(victim.thingIDNumber) && victim.HostileTo(caster))
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, PathDamage, PathArmorPen, -1f, caster);
                        victim.TakeDamage(dinfo);
                        hitPawnIds.Add(victim.thingIDNumber);
                        FleckMaker.ThrowMicroSparks(victim.DrawPos, map);
                    }
                }
            }

            if (AyayaDefOf.Raven_Projectile_WindBlade != null)
            {
                this.LaunchDanmakuAtDestination(caster, destCell, map);
            }

            return JumpUtility.DoJump(caster, this.currentTarget, base.ReloadableCompSource, this.verbProps, this.ability, base.CurrentTarget, this.JumpFlyerDef);
        }

        private void LaunchDanmakuAtDestination(Pawn caster, IntVec3 center, Map map)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, DanmakuRadius, true))
            {
                if (!cell.InBounds(map)) continue;
                List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    if (things[i] is Pawn target && target != caster && target.HostileTo(caster) && !target.Dead)
                    {
                        Projectile proj = (Projectile)GenSpawn.Spawn(AyayaDefOf.Raven_Projectile_WindBlade, caster.Position, map, WipeMode.Vanish);
                        proj.Launch(caster, caster.DrawPos, target, target, ProjectileHitFlags.IntendedTarget);
                    }
                }
            }
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            EnsureAbilityBound();

            if (this.caster == null || this.caster.Map == null) return;
            Pawn casterPawn = this.CasterPawn;
            Map map = this.caster.Map;
            IntVec3 casterPos = this.caster.Position;

            GenDraw.DrawRadiusRing(
                casterPos,
                this.EffectiveRange,
                Color.white,
                (IntVec3 c) => casterPawn != null && GenSight.LineOfSight(casterPos, c, map) && JumpUtility.ValidJumpTarget(casterPawn, map, c)
            );

            if (target.IsValid)
            {
                IntVec3 destCell = target.Cell;
                Color pathColor = GenSight.LineOfSight(casterPos, destCell, map) ? Color.green : Color.red;
                GenDraw.DrawFieldEdges(GenSight.BresenhamCellsBetween(casterPos, destCell), pathColor);
                GenDraw.DrawRadiusRing(destCell, DanmakuRadius, Color.red);

                if (casterPawn != null && JumpUtility.ValidJumpTarget(casterPawn, map, destCell))
                {
                    GenDraw.DrawTargetHighlightWithLayer(destCell.ToVector3Shifted(), AltitudeLayer.MetaOverlays);
                }
            }
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            EnsureAbilityBound();
            if (this.CasterPawn == null) return false;
            return base.CanHitTargetFrom(root, targ);
        }

        public override bool HidePawnTooltips
        {
            get
            {
                EnsureAbilityBound();
                if (this.ability == null) return false;
                return base.HidePawnTooltips;
            }
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            EnsureAbilityBound();
            if (this.ability == null) return false;
            return base.ValidateTarget(target, showMessages);
        }

        public override void OnGUI(LocalTargetInfo target)
        {
            EnsureAbilityBound();
            if (this.ability == null) return;
            base.OnGUI(target);
        }

        /// <summary>
        /// Targeter 会通过 TargetingSource 获取属性，我们也必须保护这些入口
        /// </summary>
        public override bool MultiSelect
        {
            get
            {
                EnsureAbilityBound();
                return base.MultiSelect;
            }
        }
    }
}