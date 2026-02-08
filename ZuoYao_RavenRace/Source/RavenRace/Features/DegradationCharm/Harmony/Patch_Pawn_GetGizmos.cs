using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RavenRace.Features.DegradationCharm.Harmony
{
    /// <summary>
    /// 这是实现“携带时出现按钮”的绝对核心补丁。
    /// 它挂钩到Pawn的GetGizmos方法，动态地为携带符咒的Pawn添加指令。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos
    {
        // 静态构造函数中缓存贴图，避免每帧都加载，提高性能
        private static readonly Texture2D GizmoIcon = ContentFinder<Texture2D>.Get("Items/Misc/DegradationCharm/TalismanOfCorruption_Item");

        /// <summary>
        /// 在原版的Pawn.GetGizmos()方法执行后运行，向其结果中添加我们的自定义Gizmo。
        /// </summary>
        /// <param name="values">原版方法返回的所有Gizmo。</param>
        /// <param name="__instance">被选中的Pawn实例。</param>
        /// <returns>包含原版Gizmo和我们新增Gizmo的集合。</returns>
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            // 首先，原封不动地返回所有原版的Gizmo
            foreach (var g in values)
            {
                yield return g;
            }

            // 检查当前Pawn是否符合显示Gizmo的条件：是玩家可控的殖民者，且没有倒地
            if (__instance != null && __instance.IsColonistPlayerControlled && !__instance.Downed)
            {
                // 在Pawn的物品栏（inventory）中查找我们的符咒
                // 通过检查物品是否拥有我们自定义的CompApplyCharm组件来识别
                Thing charm = __instance.inventory?.innerContainer?.FirstOrDefault(t => t.TryGetComp<Comps.CompApplyCharm>() != null);

                // 如果在物品栏中找到了符咒
                if (charm != null)
                {
                    // 创建一个新的目标指令按钮
                    var command = new Command_Target
                    {
                        defaultLabel = "贴上淫堕符咒...",
                        defaultDesc = "将一枚淫堕符咒贴在目标身上，开启其堕落之路。这会消耗一枚符咒。",
                        icon = GizmoIcon,
                        // 定义目标选择规则
                        targetingParams = new TargetingParameters
                        {
                            canTargetPawns = true,
                            canTargetBuildings = false,
                            mapObjectTargetsMustBeAutoAttackable = false, // 允许选择友方
                            validator = (TargetInfo t) =>
                            {
                                // [最终最终的修正] 从 TargetInfo 中获取 Pawn 的正确方式是 t.Thing as Pawn。
                                // 我为之前的错误深感抱歉。
                                Pawn targetPawn = t.Thing as Pawn;

                                // 目标必须是Pawn，是类人生物，且当前没有“堕落刻印”状态
                                return targetPawn != null && targetPawn.RaceProps.Humanlike && !targetPawn.health.hediffSet.HasHediff(DegradationCharmDefOf.Raven_Hediff_Degradation);
                            }
                        },
                        // 定义选择目标后执行的动作
                        action = delegate (LocalTargetInfo target)
                        {
                            // 创建并派发“贴符”工作
                            // TargetA是目标Pawn，TargetB是消耗的符咒物品
                            Job job = JobMaker.MakeJob(DegradationCharmDefOf.Raven_Job_ApplyCharm, target.Thing, charm);
                            job.count = 1;
                            __instance.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        }
                    };
                    yield return command;
                }
            }
        }
    }
}