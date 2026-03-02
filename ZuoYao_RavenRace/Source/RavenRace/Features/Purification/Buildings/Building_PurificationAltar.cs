using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using RavenRace.Features.Bloodline;

namespace RavenRace.Features.Purification
{
    /// <summary>
    /// 纯化仪式测试建筑。
    /// 核心功能是通过 FloatMenu 提供对符合条件的渡鸦进行血脉突破的选项。
    /// </summary>
    [StaticConstructorOnStartup]
    public class Building_PurificationAltar : Building
    {
        private static readonly Texture2D RitualIcon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", true);

        /// <summary>
        /// 提供底部 Gizmo 按钮。由于突破是针对个人的，通常通过选择人然后右键建筑来操作更符合原版逻辑。
        /// 但为了方便测试，我们在建筑底部也放一个下拉列表。
        /// </summary>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;

            yield return new Command_Action
            {
                defaultLabel = "进行纯化...",
                defaultDesc = "选择一名浓度已达到瓶颈的渡鸦族进行突破仪式。",
                icon = RitualIcon,
                action = () =>
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();

                    foreach (Pawn p in this.Map.mapPawns.FreeColonists)
                    {
                        if (p.Dead || p.Downed || p.def.defName != "Raven_Race") continue;

                        // [核心修复] 改为获取全新的金乌纯化组件，而非杂交血脉组件
                        var comp = p.TryGetComp<CompPurification>();
                        if (comp == null) continue;

                        // 获取他当前阶段的物理上限
                        float currentMaxLimit = comp.GetMaxConcentrationLimit();

                        // 只有他的实际浓度 >= 上限时，才允许突破
                        if (comp.GoldenCrowConcentration < currentMaxLimit)
                        {
                            options.Add(new FloatMenuOption($"{p.LabelShort} (浓度不足: {comp.GoldenCrowConcentration:P0}/{currentMaxLimit:P0})", null));
                        }
                        else
                        {
                            options.Add(new FloatMenuOption($"{p.LabelShort} (准备就绪)", () =>
                            {
                                Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_PurificationRitual, this);
                                p.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            }));
                        }
                    }

                    if (options.Count == 0) options.Add(new FloatMenuOption("没有符合条件的渡鸦族", null));

                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
        }

        /// <summary>
        /// 支持玩家选中渡鸦小人后，右键点击此建筑进行交互。
        /// </summary>
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var opt in base.GetFloatMenuOptions(selPawn)) yield return opt;

            if (selPawn.def.defName != "Raven_Race") yield break;

            // [核心修复] 改为获取全新的金乌纯化组件
            var comp = selPawn.TryGetComp<CompPurification>();
            if (comp == null) yield break;

            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("无法到达基座", null);
                yield break;
            }

            float currentMaxLimit = comp.GetMaxConcentrationLimit();

            if (comp.GoldenCrowConcentration < currentMaxLimit)
            {
                yield return new FloatMenuOption($"纯化仪式 (需要 {currentMaxLimit:P0} 金乌浓度)", null);
            }
            else
            {
                yield return new FloatMenuOption("开启纯化仪式", () =>
                {
                    Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_PurificationRitual, this);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }
    }
}