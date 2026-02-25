using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.Bionics.RavenFluidAccelerator
{
    /// <summary>
    /// UI 贴图加载器，使用 StaticConstructorOnStartup 确保在主线程加载。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class FluidAcceleratorTex
    {
        public static readonly Texture2D PopIcon = ContentFinder<Texture2D>.Get("UI/Commands/Raven_FluidPop", true);
    }

    /// <summary>
    /// 液体促进器的属性类
    /// </summary>
    public class HediffCompProperties_FluidAccelerator : HediffCompProperties
    {
        public float radius = 5.9f;
        public int cooldownTicks = 30000; // 默认半天冷却

        public HediffCompProperties_FluidAccelerator()
        {
            this.compClass = typeof(HediffComp_FluidAccelerator);
        }
    }

    /// <summary>
    /// 液体促进器组件逻辑：自动检测火焰 + UI手动触发
    /// </summary>
    public class HediffComp_FluidAccelerator : HediffComp
    {
        public HediffCompProperties_FluidAccelerator Props => (HediffCompProperties_FluidAccelerator)props;

        private int lastPopTick = -99999;

        private bool IsOnCooldown => Find.TickManager.TicksGame - lastPopTick < Props.cooldownTicks;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastPopTick, "lastPopTick", -99999);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (!Pawn.Spawned || Pawn.Dead) return;

            // 每 60 tick (1秒) 检查一次，节省性能
            if (Pawn.IsHashIntervalTick(60))
            {
                if (!IsOnCooldown && CheckAutoPopCondition())
                {
                    DoPop();
                }
            }
        }

        /// <summary>
        /// 扫描自身周围判定是否需要自动喷射
        /// </summary>
        private bool CheckAutoPopCondition()
        {
            if (Pawn.HasAttachment(ThingDefOf.Fire)) return true;

            int numCells = GenRadial.NumCellsInRadius(3f);
            Map map = Pawn.Map;

            for (int i = 0; i < numCells; i++)
            {
                IntVec3 c = Pawn.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(map))
                {
                    List<Thing> thingList = c.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j].def == ThingDefOf.Fire || thingList[j].HasAttachment(ThingDefOf.Fire))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 执行喷射逻辑，产生灭火爆炸并生成粉色体液
        /// </summary>
        private void DoPop()
        {
            lastPopTick = Find.TickManager.TicksGame;
            Map map = Pawn.Map;
            IntVec3 pos = Pawn.Position;

            // 飘字
            MoteMaker.ThrowText(Pawn.DrawPos, map, "齁哦哦哦哦哦❤", Color.magenta);

            // 【核心修复】使用正确的命名参数 explosionSound，并补全必要的参数以匹配原版 API
            GenExplosion.DoExplosion(
                center: pos,
                map: map,
                radius: Props.radius,
                damType: DamageDefOf.Extinguish,
                instigator: Pawn,
                damAmount: -1,
                armorPenetration: -1f,
                explosionSound: SoundDefOf.Explosion_FirefoamPopper,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: RavenDefOf.Filth_RavenBodilyFluid,
                postExplosionSpawnChance: 1f,
                postExplosionSpawnThingCount: 1,
                applyDamageToExplosionCellsNeighbors: true
            );
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Pawn.Faction == Faction.OfPlayer)
            {
                Command_Action action = new Command_Action
                {
                    defaultLabel = "喷射特殊体液",
                    defaultDesc = "手动激发液体促进器，因无法控制的极乐强制向周围大范围喷射极其浓浊的体液泡沫，能够有效扑灭周围的火焰。",
                    icon = FluidAcceleratorTex.PopIcon,
                    action = () =>
                    {
                        DoPop();
                    }
                };

                if (IsOnCooldown)
                {
                    int ticksLeft = Props.cooldownTicks - (Find.TickManager.TicksGame - lastPopTick);
                    action.Disable("冷却中: " + ticksLeft.ToStringTicksToPeriod());
                }

                yield return action;
            }
        }
    }
}