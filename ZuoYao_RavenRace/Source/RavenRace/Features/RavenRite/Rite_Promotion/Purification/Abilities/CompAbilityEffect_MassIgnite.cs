using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RavenRace.Features.RavenRite.Rite_Promotion.Purification.Abilities
{
    /// <summary>
    /// 群体点燃技能的属性定义
    /// 在XML中挂载于 <comps> 内。
    /// </summary>
    public class CompProperties_AbilityMassIgnite : CompProperties_AbilityEffect
    {
        public float radius = 5.9f;
        public float fireSize = 1.0f;

        public CompProperties_AbilityMassIgnite()
        {
            this.compClass = typeof(CompAbilityEffect_MassIgnite);
        }
    }

    /// <summary>
    /// 群体点燃的执行逻辑
    /// 寻找目标范围内的所有敌方单位，并在它们可燃的情况下附加火焰。
    /// </summary>
    public class CompAbilityEffect_MassIgnite : CompAbilityEffect
    {
        public new CompProperties_AbilityMassIgnite Props => (CompProperties_AbilityMassIgnite)this.props;

        /// <summary>
        /// 技能释放时调用的核心方法
        /// </summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = this.parent.pawn;
            Map map = caster.Map;
            if (map == null) return;

            IntVec3 center = target.Cell;

            // 获取目标范围内所有的格子
            IEnumerable<IntVec3> radialCells = GenRadial.RadialCellsAround(center, Props.radius, true);

            foreach (IntVec3 cell in radialCells)
            {
                if (!cell.InBounds(map)) continue;

                // 获取该格子上的所有物体
                List<Thing> thingList = cell.GetThingList(map);

                // 倒序遍历以安全进行可能修改列表的操作
                for (int i = thingList.Count - 1; i >= 0; i--)
                {
                    Thing t = thingList[i];

                    // 筛选：是 Pawn，不是自己，没有死，且属于敌对派系
                    if (t is Pawn p && p != caster && !p.Dead && p.HostileTo(caster.Faction))
                    {
                        // 使用原版核心工具判断其是否能够附加火焰且当前易燃
                        if (p.CanEverAttachFire() && p.FlammableNow)
                        {
                            p.TryAttachFire(Props.fireSize, caster);

                            // 在目标身上播放火花效果作为反馈
                            FleckMaker.ThrowMicroSparks(p.DrawPos, map);
                        }
                    }
                }
            }

            // 技能释放在中心点生成大范围的视觉热浪
            FleckMaker.ThrowHeatGlow(center, map, Props.radius);

            // 【修复编译错误】：改用安全的字符串获取原版音效，如果后续你想换自定义音效，改这里的名字即可
            SoundDef sound = DefDatabase<SoundDef>.GetNamed("Shot_IncendiaryLauncher", false);
            if (sound != null)
            {
                sound.PlayOneShot(new TargetInfo(center, map));
            }
        }

        /// <summary>
        /// 在玩家使用该技能选择目标时，绘制影响半径预览圈
        /// </summary>
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(target.Cell, Props.radius);
        }
    }
}