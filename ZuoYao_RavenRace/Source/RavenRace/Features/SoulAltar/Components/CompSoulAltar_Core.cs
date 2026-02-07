using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using RavenRace.Features.Reproduction; // [关键修复] 添加此引用以识别 CompSpiritEgg

namespace RavenRace
{
    public class CompProperties_SoulAltar : CompProperties
    {
        public float hatchingSpeedPerPylon = 0.08f;
        public CompProperties_SoulAltar() => compClass = typeof(CompSoulAltar);
    }

    /// <summary>
    /// 扶桑育生祭坛核心组件 - 核心定义与生命周期
    /// </summary>
    public partial class CompSoulAltar : ThingComp
    {
        public CompProperties_SoulAltar Props => (CompProperties_SoulAltar)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // 初始化时扫描一次网络，建立连接
            ScanNetwork();
        }

        public override void CompTick()
        {
            base.CompTick();

            // 1. 孵化逻辑
            // 只有当作为 Building_Cradle 存在且有蛋时才运行
            if (parent is Building_Cradle cradle && cradle.GetDirectlyHeldThings().Count > 0)
            {
                // [修复 CS0246] 现在可以正确识别 CompSpiritEgg 了
                var egg = cradle.GetDirectlyHeldThings()[0].TryGetComp<CompSpiritEgg>();

                // 只有处于孵化状态才推进
                if (egg != null && egg.isIncubating)
                {
                    // 计算加速倍率 (定义在 Network 部分)
                    float speedMult = GetSpeedMultiplier();

                    // 调用 SpiritEgg 的自定义 Tick
                    egg.TickIncubation(speedMult);
                }
            }

            // 2. 网络维护
            // 降低扫描频率，每 250 tick (约4秒) 扫描一次网络连接，节省性能
            if (parent.IsHashIntervalTick(250))
            {
                ScanNetwork();
            }
        }

        public override string CompInspectStringExtra()
        {
            // 可以在这里显示简单的连接状态，或者留给 Dialog 显示
            return base.CompInspectStringExtra();
        }
    }
}