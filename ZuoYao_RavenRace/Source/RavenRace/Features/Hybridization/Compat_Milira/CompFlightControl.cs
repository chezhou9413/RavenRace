// File: Source/RavenRace/Features/Hybridization/Compat_Milira/CompFlightControl.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Compat.Milira
{
    /// <summary>
    /// 伪装成米莉拉的 CompFlightControl。
    /// 
    /// 【核心原理】
    /// 天羽族补丁通过 c.GetType().Name == "CompFlightControl" 来查找组件。
    /// 我们必须使用相同的类名 "CompFlightControl"，但在不同的命名空间下。
    /// 这样既能骗过天羽族补丁开启穿墙寻路，又不会与原版米莉拉代码冲突。
    /// </summary>
    public class CompFlightControl : ThingComp
    {
        // ===================================================
        // 天羽族补丁反射读取的核心字段 (必须保持字段名一致)
        // ===================================================
        public bool switchOn = false;
        public bool onlyForMove = false;

        // ===================================================
        // 模拟米莉拉的 Props 结构
        // ===================================================
        public class PropsProxy
        {
            public float hungerPctThresholdCanFly = 0.1f;
            public float hungerPctCostPerSecondFly = 0.001f;
            public BodyPartDef bodyPart = null;
        }

        private PropsProxy propsCache;
        private Pawn Pawn => parent as Pawn;

        // 我们自己的飞行Hediff，属性与米莉拉一致，但逻辑由我们控制
        private static HediffDef flightHediffDef;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (propsCache == null) propsCache = new PropsProxy();

            if (flightHediffDef == null)
            {
                // 使用我们自己定义的 Hediff，防止米莉拉原版 Hediff 报错
                flightHediffDef = DefDatabase<HediffDef>.GetNamed("Raven_Hediff_MiliraFlight");
            }
        }

        // 欺骗反射读取 props
        public new object props => propsCache ?? (propsCache = new PropsProxy());

        public override void CompTick()
        {
            base.CompTick();
            if (Pawn == null || !Pawn.Spawned) return;

            // 每 60 tick (1秒) 执行一次逻辑
            if (Pawn.IsHashIntervalTick(60))
            {
                bool isFlying = IsActuallyFlying();

                // 1. 管理飞行 Hediff (提供闪避率和Buff)
                if (flightHediffDef != null)
                {
                    bool hasHediff = Pawn.health.hediffSet.HasHediff(flightHediffDef);
                    if (isFlying && !hasHediff)
                    {
                        Pawn.health.AddHediff(flightHediffDef);
                    }
                    else if (!isFlying && hasHediff)
                    {
                        Hediff h = Pawn.health.hediffSet.GetFirstHediffOfDef(flightHediffDef);
                        if (h != null) Pawn.health.RemoveHediff(h);
                    }
                }

                // 2. 消耗饥饿
                if (isFlying && switchOn && Pawn.needs?.food != null)
                {
                    Pawn.needs.food.CurLevelPercentage -= propsCache.hungerPctCostPerSecondFly;
                }
            }
        }

        public bool IsActuallyFlying()
        {
            if (!switchOn) return false;

            // 饥饿检查
            if (Pawn.needs?.food != null && Pawn.needs.food.CurLevelPercentage < propsCache.hungerPctThresholdCanFly)
                return false;

            // 高空模式：常驻飞行
            if (!onlyForMove) return true;

            // 低空模式：仅移动或跳跃时飞行
            if (Pawn.pather != null && Pawn.pather.Moving) return true;
            if (Pawn.Flying) return true;

            return false;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Pawn.Faction != Faction.OfPlayer) yield break;

            // 尝试加载米莉拉的图标，如果失败使用原版图标
            Texture2D iconToggle = ContentFinder<Texture2D>.Get("Milira/Faction/Faction_Icon", false)
                                   ?? ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", true);

            yield return new Command_Toggle
            {
                defaultLabel = "Milira_Flight_Toggle".Translate(),
                defaultDesc = "Milira_Flight_Toggle_Desc".Translate(),
                icon = iconToggle,
                isActive = () => switchOn,
                toggleAction = () => switchOn = !switchOn,
                hotKey = KeyBindingDefOf.Misc1
            };

            if (switchOn)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = onlyForMove ? "Milira_Flight_LowAlt".Translate() : "Milira_Flight_HighAlt".Translate(),
                    defaultDesc = "Milira_Flight_Mode_Desc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/PlanOff", true), // 原版图标，安全
                    isActive = () => onlyForMove,
                    toggleAction = () => onlyForMove = !onlyForMove,
                    hotKey = KeyBindingDefOf.Misc2
                };
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref switchOn, "switchOn", false);
            Scribe_Values.Look(ref onlyForMove, "onlyForMove", false);
        }
    }

    // 对应的 Props 类，名字无所谓，只要类型匹配即可
    public class CompProperties_FlightControl : CompProperties
    {
        public CompProperties_FlightControl()
        {
            this.compClass = typeof(CompFlightControl);
        }
    }
}