using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RavenRace
{
    public partial class CompSoulAltar
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // 1. 打开主控界面
            yield return new Command_Action
            {
                defaultLabel = "主控界面",
                defaultDesc = "打开祭坛连接与状态监控面板。",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Inspect", true),
                action = () => Find.WindowStack.Add(new Dialog_SoulAltar(this))
            };

            // 2. 归元回溯 (新逻辑)
            // 检查：属于玩家 + 科技解锁 + 有蛋
            bool techUnlocked = DefDatabase<ResearchProjectDef>.GetNamed("RavenResearch_BloodlineRegression", false)?.IsFinished == true;
            bool hasEgg = HasSpiritEgg();

            if (this.parent.Faction == Faction.OfPlayer && techUnlocked)
            {
                Command_Action ritualCmd = new Command_Action
                {
                    defaultLabel = "归元回溯",
                    defaultDesc = "开始归元回溯仪式，吞噬摇篮中的灵卵以强化血脉。\n\n需要：\n- 祭坛中有灵卵\n- 一名渡鸦族殖民者",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/Raven_Ritual_Absorption", true),
                    action = () =>
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();

                        // 寻找可用的发起者
                        foreach (Pawn p in this.parent.Map.mapPawns.FreeColonists)
                        {
                            if (p.Dead || p.Downed || p.def.defName != "Raven_Race") continue;

                            if (!p.CanReach(this.parent, PathEndMode.InteractionCell, Danger.Deadly))
                            {
                                options.Add(new FloatMenuOption($"{p.LabelShort} (无法到达)", null));
                                continue;
                            }

                            options.Add(new FloatMenuOption(p.LabelShort, () =>
                            {
                                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Raven_Job_BloodlineRitual"), this.parent);
                                p.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            }));
                        }

                        if (options.Count == 0)
                        {
                            options.Add(new FloatMenuOption("无可用渡鸦族", null));
                        }

                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };

                // 如果没蛋，禁用按钮
                if (!hasEgg)
                {
                    ritualCmd.Disable("RavenRace_Ritual_CradleEmpty".Translate());
                }

                yield return ritualCmd;
            }
        }

        private bool HasSpiritEgg()
        {
            if (parent is Building_Cradle cradle)
            {
                if (cradle.GetDirectlyHeldThings().Count > 0)
                {
                    return cradle.GetDirectlyHeldThings()[0].def.defName == "Raven_SpiritEgg";
                }
            }
            return false;
        }
    }
}