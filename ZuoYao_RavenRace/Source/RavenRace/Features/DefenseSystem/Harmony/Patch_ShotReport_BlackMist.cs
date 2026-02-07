using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.DefenseSystem.Harmony
{
    /// <summary>
    /// 拦截命中率报告，实现“黑灵迷雾”的单向烟逻辑。
    /// 
    /// 性能优化说明：
    /// 1. ShotReport 是结构体(struct)，通过 ref 传递。为了修改它，必须使用装箱(Boxing)技术。
    /// 2. 使用静态只读的 FieldInfo 缓存反射信息，避免每帧查找的开销。
    /// 3. 将反射写入操作放在所有逻辑判断之后，只有在确实需要修改数据时才执行，最大程度降低性能影响。
    /// </summary>
    [HarmonyPatch(typeof(ShotReport), "HitReportFor")]
    public static class Patch_ShotReport_BlackMist
    {
        // [性能] 缓存反射字段信息。
        // 请确认 RimWorld 1.6 中 ShotReport 类确实包含名为 "factorFromCoveringGas" 的私有字段。
        private static readonly FieldInfo factorField = AccessTools.Field(typeof(ShotReport), "factorFromCoveringGas");

        /// <summary>
        /// 后置补丁：在原版计算完命中率后，根据黑灵烟雾的存在修正气体掩护因子。
        /// </summary>
        /// <param name="caster">射击者</param>
        /// <param name="verb">使用的动作/武器</param>
        /// <param name="target">目标</param>
        /// <param name="__result">原版计算出的命中率报告 (ref 传递的结构体)</param>
        [HarmonyPostfix]
        public static void Postfix(Thing caster, Verb verb, LocalTargetInfo target, ref ShotReport __result)
        {
            // 1. [快速过滤] 如果地图上没有黑灵迷雾（或者该功能未实装），或者 Caster 无效，直接退出。
            // 这里的 caster.Map 检查非常快。
            if (caster == null || caster.Map == null) return;

            // 2. 获取射击线。如果射击线无效，退出。
            // 注意：此时我们还没有进行反射读取，开销很小。
            var shootLine = __result.ShootLine;
            if (shootLine.Dest == IntVec3.Invalid) return;

            // 3. [耗时操作] 检查路径上是否有特定烟雾。
            // 这是必须的遍历，无法省略。
            bool passThroughMist = false;

            // 使用 DefOf 避免硬编码字符串查找
            ThingDef mistDef = RavenDefOf.RavenGas_BlackMist;
            if (mistDef == null) return; // 防御性编程

            foreach (IntVec3 cell in shootLine.Points())
            {
                if (cell.InBounds(caster.Map))
                {
                    // 检查该格子上是否有黑灵烟雾
                    // GetFirstThing 比 GetThingList 更快，因为它找到一个就返回
                    var mist = cell.GetFirstThing(caster.Map, mistDef);
                    if (mist != null)
                    {
                        passThroughMist = true;
                        break;
                    }
                }
            }

            // 如果路径上没有烟雾，不做任何修改，直接返回
            if (!passThroughMist) return;

            // 4. 判定敌我 (是否豁免惩罚)
            bool isFriendly = false;

            if (caster.Faction == Faction.OfPlayer)
            {
                isFriendly = true;
            }
            // 渡鸦族生物（包括盟友）也豁免。使用 DefOf 进行比较。
            else if (caster.def == RavenDefOf.Raven_Race && !caster.HostileTo(Faction.OfPlayer))
            {
                isFriendly = true;
            }

            // 5. [核心修改] 修改命中率因子
            // 由于 __result 是 ref struct，我们必须先装箱(Box)成 object 才能使用 FieldInfo.GetValue/SetValue
            if (factorField != null)
            {
                object boxedResult = __result;

                // 读取当前值
                float currentFactor = (float)factorField.GetValue(boxedResult);

                bool changed = false;

                if (isFriendly)
                {
                    // 己方：无视烟雾 (恢复为 1.0f)
                    // 只有当因子受到惩罚（小于1）时才需要恢复
                    if (currentFactor < 0.99f)
                    {
                        factorField.SetValue(boxedResult, 1.0f);
                        changed = true;
                    }
                }
                else
                {
                    // 敌方：极大惩罚 (设为 0.1f)
                    // 只有当因子还比较高（大于0.1）时才需要惩罚
                    if (currentFactor > 0.11f)
                    {
                        factorField.SetValue(boxedResult, 0.1f);
                        changed = true;
                    }
                }

                // 只有在数据真正改变时，才执行拆箱(Unbox)并赋值回 ref 参数
                if (changed)
                {
                    __result = (ShotReport)boxedResult;
                }
            }
        }
    }
}