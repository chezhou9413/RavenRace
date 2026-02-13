using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;
using RimWorld;
using rjw;
using rjw.Modules.Interactions;

namespace RavenRace.RJWCompat
{
    /// <summary>
    /// 拦截 RJW 的互动选择逻辑，强制应用玩家在对话框中的选择。
    /// </summary>
    [HarmonyPatch]
    public static class Patch_SexInteractionHelper
    {
        // 动态获取目标方法
        static MethodBase TargetMethod()
        {
            // 目标方法是 SexInteractionHelper.ChooseInteraction(SexProps props)
            Type helperType = AccessTools.TypeByName("rjw.Modules.Interactions.SexInteractionHelper");
            if (helperType == null)
            {
                Log.Warning("[RavenRace RJWCompat] 找不到 RJW 的 SexInteractionHelper 类。");
                return null;
            }

            MethodInfo method = AccessTools.Method(helperType, "ChooseInteraction");
            if (method == null)
            {
                Log.Warning("[RavenRace RJWCompat] 找不到 SexInteractionHelper.ChooseInteraction 方法。");
            }
            return method;
        }

        // 在 ChooseInteraction 执行前拦截
        [HarmonyPrefix]
        static bool Prefix(SexProps props, ref bool __result)
        {
            try
            {
                // 1. 获取当前正在执行的 Job
                Job currentJob = props?.pawn?.CurJob;
                if (currentJob == null) return true; // 没有Job，让RJW正常处理

                // 2. 检查 job.interaction 是否包含我们存储的 InteractionDef
                InteractionDef selectedInteraction = currentJob.interaction;
                if (selectedInteraction == null) return true; // 没有我们的标记，让RJW正常处理

                // 3. 验证这个 InteractionDef 是否确实是 RJW 的性交互动
                var extension = selectedInteraction.GetModExtension<SexInteractionExtension>();
                if (extension == null)
                {
                    // 不是 RJW 互动，让RJW正常处理
                    return true;
                }

                Log.Message($"[RavenRace RJWCompat] 检测到玩家选择的互动: {selectedInteraction.defName}，开始强制应用...");

                // 4. 创建 SexInteraction 对象并强制设置到 SexProps
                var sexInteraction = new SexInteraction(selectedInteraction);
                props.interaction = sexInteraction;

                // 5. 使用 RJW 的内部逻辑来解析这个互动的具体部位配对
                // 这确保了即使 RJW 更新，我们的逻辑也能保持兼容
                var cache = new SexInteractionFinder.Internal.FinderCache(props);
                var resolvedList = SexInteractionFinder.Internal.ResolveInteraction(sexInteraction, props, cache).ToList();

                if (resolvedList.Count > 0)
                {
                    // 强制使用第一个解析结果（通常只有一个）
                    props.resolved = resolvedList[0];

                    Log.Message($"[RavenRace RJWCompat] 成功强制应用互动: {selectedInteraction.defName} (sexType={sexInteraction.Sextype})");

                    // 清空 job.interaction 避免后续干扰（虽然不太可能有问题）
                    currentJob.interaction = null;

                    // 设置返回值为 true，表示互动选择成功
                    __result = true;

                    // 返回 false 阻止 RJW 的 ChooseInteraction 执行
                    return false;
                }
                else
                {
                    Log.Warning($"[RavenRace RJWCompat] 玩家选择的互动 {selectedInteraction.defName} 无法解析出有效的部位配对，回退到 RJW 自动选择。");
                    currentJob.interaction = null;
                    return true; // 让RJW重新选择
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace RJWCompat] 在强制应用互动时出错: {ex}");
                return true; // 出错时让RJW正常处理，避免崩溃
            }
        }
    }
}