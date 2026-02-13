using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace RavenRace.RJWCompat
{
    /// <summary>
    /// 补丁 RJW 的 JobDriver_SexQuick（对应 "Quickie" JobDef），
    /// 确保玩家在对话框中选择的 sexType 被正确应用。
    /// </summary>
    [HarmonyPatch]
    public static class Patch_JobDriver_SexQuick
    {
        // 动态获取目标方法（因为我们不能直接引用 RJW 的类）
        static MethodBase TargetMethod()
        {
            Type jobDriverType = AccessTools.TypeByName("rjw.JobDriver_SexQuick");
            if (jobDriverType == null)
            {
                Log.Warning("[RavenRace RJWCompat] 找不到 RJW 的 JobDriver_SexQuick 类。SexType 强制应用补丁将不会生效。");
                return null;
            }

            // Patch MakeNewToils 方法（JobDriver 的初始化入口）
            MethodBase method = AccessTools.Method(jobDriverType, "MakeNewToils");
            if (method == null)
            {
                Log.Warning("[RavenRace RJWCompat] 找不到 JobDriver_SexQuick.MakeNewToils 方法。");
            }
            return method;
        }

        // 在 MakeNewToils 执行前拦截，注入我们的 sexType
        [HarmonyPrefix]
        static void Prefix(JobDriver __instance)
        {
            try
            {
                // 1. 检查 job.loadID 是否包含我们存储的 sexType（范围 0-20 是合理的）
                if (__instance.job.loadID < 0 || __instance.job.loadID > 20)
                {
                    // 没有我们的标记，说明这是原版RJW触发的Job，不干预
                    return;
                }

                int desiredSexTypeInt = __instance.job.loadID;

                // 2. 获取 RJW 的 rjwSextype 枚举类型
                Type sexTypeEnum = AccessTools.TypeByName("xxx+rjwSextype");
                if (sexTypeEnum == null)
                {
                    Log.Error("[RavenRace RJWCompat] 找不到 RJW 的 'xxx.rjwSextype' 枚举类型。");
                    return;
                }

                // 3. 将我们的整数转换为 RJW 的枚举值
                object desiredSexType = Enum.ToObject(sexTypeEnum, desiredSexTypeInt);

                // 4. 通过反射找到 JobDriver 的 sexType 字段（它在父类 JobDriver_Sex 中）
                Type baseDriverType = AccessTools.TypeByName("rjw.JobDriver_Sex");
                if (baseDriverType == null)
                {
                    Log.Error("[RavenRace RJWCompat] 找不到 RJW 的 JobDriver_Sex 基类。");
                    return;
                }

                FieldInfo sexTypeField = AccessTools.Field(baseDriverType, "sexType");
                if (sexTypeField == null)
                {
                    Log.Error("[RavenRace RJWCompat] 在 JobDriver_Sex 中找不到 'sexType' 字段。");
                    return;
                }

                // 5. 强制设置 sexType 字段
                sexTypeField.SetValue(__instance, desiredSexType);

                Log.Message($"[RavenRace RJWCompat] 成功将 sexType 强制设置为 {desiredSexType} (玩家选择)。");

                // 6. 清空 loadID，避免后续保存/加载时产生干扰
                __instance.job.loadID = -1;
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace RJWCompat] 在注入 sexType 时出错: {ex}");
            }
        }
    }
}