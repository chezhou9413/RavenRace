using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using rjw;

namespace RavenRace.RJWCompat
{
    /// <summary>
    /// 全局性爱/色情行为拦截器 (RJW兼容)。
    /// 包含两个核心功能：
    /// 1. 任务执行期间：每秒平滑、缓慢地恢复性需求（完美模仿原版娱乐和休息的体验）。
    /// 2. 任务结束时：对“交配”行为触发 RJW 的交配结算 (Aftersex)，而“色情发泄”行为则不触发。
    /// </summary>
    [HarmonyPatch]
    public static class Patch_SexualJobs_RJW
    {
        // =====================================================================
        // 功能 1：在任务进行中缓慢平滑地恢复性需求 (每秒触发一次，极低性能开销)
        // =====================================================================
        [HarmonyPatch(typeof(JobDriver), "DriverTick")]
        [HarmonyPostfix]
        public static void DriverTick_Postfix(JobDriver __instance)
        {
            if (__instance.job?.def == null || __instance.pawn == null) return;

            // 只有在处于活动的 Toil 中才恢复
            if (__instance.ended || __instance.pawn.Dead || __instance.pawn.Downed) return;

            // 性能优化：每 60 tick (1秒) 恢复一次，而不是每一帧都算
            if (__instance.pawn.IsHashIntervalTick(60))
            {
                bool isMating = false;
                bool isErotic = false;

                // [核心修复] 跨 DLL 边界安全检查：通过类型名称字符串匹配，绝对不会报强转错误
                if (__instance.job.def.modExtensions != null)
                {
                    foreach (var ext in __instance.job.def.modExtensions)
                    {
                        string extName = ext.GetType().Name;
                        if (extName == "DefModExtension_LovinJob") isMating = true;
                        if (extName == "DefModExtension_EroticJob") isErotic = true;
                    }
                }

                if (isMating || isErotic)
                {
                    // 动态恢复量：大多数动作持续约 2000 游戏刻（约 33 秒）。
                    // 每秒增加 0.035，大约能在动作结束时刚刚好平滑加满 1.0 的需求。
                    float recoverAmount = 0.035f;

                    RecoverSexNeedGradually(__instance.pawn, recoverAmount);

                    // 如果是双人交配，连带恢复伴侣的需求
                    if (isMating)
                    {
                        Pawn partner = __instance.job.targetA.Thing as Pawn;
                        if (partner != null && partner.RaceProps.Humanlike)
                        {
                            RecoverSexNeedGradually(partner, recoverAmount);
                        }
                    }
                }
            }
        }

        // =====================================================================
        // 功能 2：任务结束时，仅针对“交配”行为触发高潮与后续机制结算
        // =====================================================================
        [HarmonyPatch(typeof(JobDriver), "Cleanup")]
        [HarmonyPrefix]
        public static void Cleanup_Prefix(JobDriver __instance, JobCondition condition)
        {
            if (__instance.job?.def == null || __instance.pawn == null) return;

            // [核心修复] 放宽结束条件。娱乐类动作经常以 InterruptForced 结束。
            // 只要不是因为报错(Errored)或寻路失败(Incompletable)结束的，都视为完成发泄
            if (condition == JobCondition.Succeeded || condition == JobCondition.InterruptForced)
            {
                bool isMating = false;
                if (__instance.job.def.modExtensions != null)
                {
                    foreach (var ext in __instance.job.def.modExtensions)
                    {
                        if (ext.GetType().Name == "DefModExtension_LovinJob")
                        {
                            isMating = true;
                            break;
                        }
                    }
                }

                // 只有被标记为“交配”的行为才需要通知 RJW 结算
                if (isMating)
                {
                    Pawn pawn = __instance.pawn;
                    Pawn partner = null;

                    if (__instance.job.targetA != null && __instance.job.targetA.HasThing)
                    {
                        partner = __instance.job.targetA.Thing as Pawn;
                    }

                    if (partner != null && partner.RaceProps.Humanlike)
                    {
                        // 原版 Lovin 和 RJW 原生 Job 会自行处理结算，所以避开它们防重复触发
                        if (!__instance.job.def.defName.StartsWith("rjw_") &&
                            __instance.job.def.defName != "Quickie" &&
                            __instance.job.def.defName != "Lovin")
                        {
                            TriggerRjwAfterSex(pawn, partner);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 安全获取 RJW 的 Need_Sex，并平滑增加数值。
        /// </summary>
        private static void RecoverSexNeedGradually(Pawn pawn, float amount)
        {
            if (pawn?.needs == null) return;

            Need_Sex sexNeed = pawn.needs.TryGetNeed<Need_Sex>();
            if (sexNeed != null)
            {
                // 使用 Mathf.Min 确保数值绝不超过 1.0 (满值)
                sexNeed.CurLevel = UnityEngine.Mathf.Min(sexNeed.CurLevel + amount, 1.0f);
            }
        }

        /// <summary>
        /// 构造虚拟参数，手动调用 RJW 的高潮结算。
        /// 解决只读属性报错：通过反射直接给 _sexType 私有字段赋值。
        /// </summary>
        private static void TriggerRjwAfterSex(Pawn initiator, Pawn partner)
        {
            try
            {
                // 构造 RJW 需要的上下文数据
                SexProps props = new SexProps
                {
                    pawn = initiator,
                    partner = partner,
                    isRape = false // 我们默认按照非强奸结算，保证不破坏原版好感体系
                };

                // [解决 CS0200 报错] 绕过只读属性 sexType，直接修改底层的私有字段 _sexType
                // 枚举值 0 对应 xxx.rjwSextype.Vaginal，填入一个合法值确保 RJW 底层不抛出空引用异常
                var sexTypeField = AccessTools.Field(typeof(SexProps), "_sexType");
                if (sexTypeField != null)
                {
                    sexTypeField.SetValue(props, new xxx.rjwSextype?(xxx.rjwSextype.Vaginal));
                }

                // 调用 RJW 的核心结算方法
                SexUtility.Aftersex(props);

                // 保留这行绿字，方便你确认它确实运行了
                Log.Message($"[RavenRace RJWCompat] 成功触发 RJW 的 Aftersex 结算: {initiator.LabelShort} 和 {partner.LabelShort}");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[RavenRace RJWCompat] 尝试手动触发 RJW Aftersex 时发生错误: {ex}");
            }
        }
    }
}