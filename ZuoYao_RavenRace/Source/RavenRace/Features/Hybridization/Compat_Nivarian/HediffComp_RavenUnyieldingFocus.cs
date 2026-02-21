using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Compat.Nivarian
{
    public class HediffCompProperties_RavenUnyieldingFocus : HediffCompProperties
    {
        public HediffCompProperties_RavenUnyieldingFocus()
        {
            this.compClass = typeof(HediffComp_RavenUnyieldingFocus);
        }
    }

    /// <summary>
    /// 复刻自 Nivarian.Gene_UnyieldingFocus
    /// 实现征召时增强战斗专注力的逻辑
    /// </summary>
    public class HediffComp_RavenUnyieldingFocus : HediffComp
    {
        // 缓存获取 Def，减少每帧调用的开销
        private HediffDef FocusHediffDef => NivarianCompatUtility.UnyieldingFocusDef;

        public override void CompPostTick(ref float severityAdjustment)
        {
            // 基础检查：每20 tick 执行一次，且 pawn 必须生成在地图上
            if (!Pawn.IsHashIntervalTick(20) || !Pawn.Spawned)
            {
                return;
            }

            // 如果原版 Def 没找到（Mod未加载或版本不匹配），直接跳过
            if (FocusHediffDef == null) return;

            // 获取或查找目标 Hediff (Nivarian_Hediff_UnyieldingFocus)
            Hediff focusHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(FocusHediffDef);

            if (Pawn.Drafted)
            {
                // 征召状态：添加或增强 Hediff
                if (focusHediff == null)
                {
                    focusHediff = Pawn.health.AddHediff(FocusHediffDef);
                    // 初始严重度通常由 HediffDef 定义，这里让它保持默认
                }

                // 增加严重度：每次 +0.002，上限 1.0
                // 原版代码: hediff.Severity = Mathf.Min(1f, hediff.Severity + 0.002f);
                focusHediff.Severity = Mathf.Min(1f, focusHediff.Severity + 0.002f);
            }
            else if (focusHediff != null)
            {
                // 非征召状态：快速衰减
                // 原版代码: hediff.Severity = Mathf.Max(0f, hediff.Severity - 0.043f);
                focusHediff.Severity = Mathf.Max(0f, focusHediff.Severity - 0.043f);

                // 如果归零，通常 Hediff 会自动移除（取决于 XML 配置），或者我们可以手动移除
                // 这里保留原版逻辑，只修改 Severity
            }

            // 播放粒子特效
            EmitParticle(focusHediff);
        }

        /// <summary>
        /// 粒子特效逻辑
        /// 复刻自 Nivarian.Gene_UnyieldingFocus.EmitParticle
        /// </summary>
        private void EmitParticle(Hediff hediff)
        {
            if (hediff == null || hediff.Severity <= 0f) return;

            // 概率播放：Severity 越高，播放概率越大
            if (UnityEngine.Random.Range(0f, 1f) <= hediff.Severity)
            {
                ThingDef moteDef = Pawn.Drafted
                    ? NivarianCompatUtility.MoteRisingDef
                    : NivarianCompatUtility.MoteDecreasingDef;

                if (moteDef != null)
                {
                    // 计算随机偏移
                    Vector3 offset = new Vector3(
                        UnityEngine.Random.Range(-0.3f, 0.3f),
                        0f,
                        UnityEngine.Random.Range(-0.25f, 0.25f)
                    );

                    // 生成 Mote
                    // 原版使用了 NivarianMoteHelper，我们使用通用的 GenSpawn
                    // 因为 Mote 也是 Thing，可以直接 Spawn
                    if (moteDef.thingClass == typeof(MoteThrown) || moteDef.thingClass == typeof(Mote))
                    {
                        MoteMaker.MakeStaticMote(Pawn.DrawPos + offset, Pawn.Map, moteDef, 1f);
                    }
                    else
                    {
                        // 兜底生成
                        GenSpawn.Spawn(moteDef, (Pawn.DrawPos + offset).ToIntVec3(), Pawn.Map);
                    }
                }
            }
        }
    }
}