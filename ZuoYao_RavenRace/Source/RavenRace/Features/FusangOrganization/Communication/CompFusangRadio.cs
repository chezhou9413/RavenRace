using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RavenRace
{
    public class CompProperties_FusangRadio : CompProperties
    {
        public CompProperties_FusangRadio()
        {
            this.compClass = typeof(CompFusangRadio);
        }
    }

    /// <summary>
    /// 扶桑电台的交互组件
    /// </summary>
    public class CompFusangRadio : ThingComp
    {
        // 缓存图标，避免每帧获取
        private static Texture2D cachedCommandIcon;

        private Texture2D CommandIcon
        {
            get
            {
                if (cachedCommandIcon == null)
                {
                    // [修复] 正确获取原版通讯台的图标
                    // 先尝试从 Def 获取
                    ThingDef commsDef = DefDatabase<ThingDef>.GetNamedSilentFail("CommsConsole");
                    if (commsDef != null)
                    {
                        cachedCommandIcon = commsDef.uiIcon;
                    }

                    // 如果失败，使用兜底图标
                    if (cachedCommandIcon == null)
                    {
                        cachedCommandIcon = BaseContent.BadTex;
                    }
                }
                return cachedCommandIcon;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var powerComp = parent.GetComp<CompPowerTrader>();
            bool hasPower = powerComp == null || powerComp.PowerOn;

            Command_Action openCmd = new Command_Action
            {
                defaultLabel = "RavenRace_OpenFusangRadio".Translate(),
                defaultDesc = "RavenRace_OpenFusangRadioDesc".Translate(),
                icon = CommandIcon, // 使用修复后的图标属性
                action = () =>
                {
                    Pawn user = FindUser();
                    if (user != null)
                    {
                        Find.WindowStack.Add(new Dialog_FusangComm(parent));
                    }
                    else
                    {
                        Messages.Message("RavenRace_NoColonistCanReach".Translate(), parent, MessageTypeDefOf.RejectInput);
                    }
                }
            };

            if (!hasPower) openCmd.Disable("NoPower".Translate());
            yield return openCmd;

            // 核心修改：添加新的开发者调试按钮
            if (DebugSettings.ShowDevGizmos)
            {
                // ---- 添加“与扶桑关系+100”按钮 ----
                var command_Goodwill = new Command_Action
                {
                    defaultLabel = "调试：关系+100",
                    defaultDesc = "立刻将玩家派系与扶桑隐世的派系关系增加100点。",
                    action = () =>
                    {
                        Faction fusangFaction = Find.FactionManager.FirstFactionOfDef(FusangDefOf.Fusang_Hidden);
                        if (fusangFaction != null)
                        {
                            Faction.OfPlayer.TryAffectGoodwillWith(fusangFaction, 100, true, false, HistoryEventDefOf.DebugGoodwill);
                            Messages.Message("调试：与扶桑隐世的关系已增加100。", MessageTypeDefOf.PositiveEvent);
                        }
                        else
                        {
                            Messages.Message("调试：未找到扶桑隐世派系。", MessageTypeDefOf.RejectInput);
                        }
                    }
                };
                yield return command_Goodwill;

                // [新增] 调试：瞬间完成任务
                yield return new Command_Action
                {
                    defaultLabel = "DEV: 完成所有任务",
                    defaultDesc = "立即完成所有正在进行的间谍任务（成功率100%）。",
                    action = () =>
                    {
                        var comp = Find.World.GetComponent<Features.Espionage.WorldComponent_Espionage>();
                        // 这里需要访问 comp 的私有任务列表，或者在 comp 里加个 Debug 方法
                        // 建议在 WorldComponent 中加 public void DebugFinishAllMissions()
                        comp.DebugFinishAllMissions();
                    }
                };

                // [新增] 调试：加满资源
                yield return new Command_Action
                {
                    defaultLabel = "DEV: 满资源",
                    action = () =>
                    {
                        FusangResourceManager.Add(FusangResourceType.Intel, 1000);
                        FusangResourceManager.Add(FusangResourceType.Influence, 1000);
                        Messages.Message("资源已加满。", MessageTypeDefOf.TaskCompletion);
                    }
                };

                // ---- 添加“强制触发主线任务”按钮 (任务名为FallenLeavesQuest，是示意性的) ----
                var command_TriggerQuest = new Command_Action
                {
                    defaultLabel = "调试：强制触发主线",
                    defaultDesc = "无视所有条件，立刻尝试触发“落叶归根”主线任务。",
                    action = () =>
                    {
                        IncidentDef questDef = DefDatabase<IncidentDef>.GetNamedSilentFail("FallenLeavesQuest");
                        if (questDef != null)
                        {
                            IncidentParms parms = StorytellerUtility.DefaultParmsNow(questDef.category, parent.Map);
                            if (questDef.Worker.TryExecute(parms))
                            {
                                Messages.Message("调试：已成功触发主线任务。", MessageTypeDefOf.PositiveEvent);
                            }
                            else
                            {
                                Messages.Message("调试：触发主线任务失败。", MessageTypeDefOf.RejectInput);
                            }
                        }
                        else
                        {
                            Messages.Message("调试：未找到 'FallenLeavesQuest' 任务定义。", MessageTypeDefOf.RejectInput);
                        }
                    }
                };
                yield return command_TriggerQuest;
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
                yield break;
            }

            var powerComp = parent.GetComp<CompPowerTrader>();
            if (powerComp != null && !powerComp.PowerOn)
            {
                yield return new FloatMenuOption("CannotUseNoPower".Translate(), null);
                yield break;
            }

            yield return new FloatMenuOption("RavenRace_OpenFusangRadio".Translate(), () =>
            {
                Job job = JobMaker.MakeJob(FusangDefOf.Raven_Job_UseFusangRadio, parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }

        private Pawn FindUser()
        {
            if (parent.Map == null) return null;
            foreach (Pawn p in parent.Map.mapPawns.FreeColonistsSpawned)
            {
                if (!p.Dead && !p.Downed && p.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
                    return p;
            }
            return null;
        }
    }
}