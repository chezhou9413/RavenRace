using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RavenRace.Features.MiscSmallFeatures.AVTelevision
{
    [StaticConstructorOnStartup]
    public class CompTV_AV : ThingComp
    {
        public bool avModeActive = false;
        private static readonly Texture2D AVIcon = ContentFinder<Texture2D>.Get("UI/Commands/WatchAV_Toggle", true);

        public IEnumerable<Pawn> CurrentWatchers => parent.Map.mapPawns.AllPawnsSpawned
            .Where(p => p.CurJob != null &&
                       (p.CurJob.def.defName == "WatchTelevision" || p.CurJob.def.defName == "Raven_WatchAV") &&
                       p.CurJob.targetA.Thing == parent);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref avModeActive, "avModeActive", false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Toggle
            {
                defaultLabel = "开启成人频道",
                defaultDesc = "激活扶桑影业加密波段。播放极度淫秽的影像，目睹阳具不断贯穿阴道、精液填满子宫等感官画面，诱发强烈的交配冲动。",
                icon = AVIcon,
                isActive = () => avModeActive,
                toggleAction = () => {
                    avModeActive = !avModeActive;
                    parent.Notify_ColorChanged();
                }
            };
        }

        public void Notify_PawnWatching(Pawn pawn)
        {
            if (!avModeActive) return;
            if (RavenDefOf.Raven_Thought_WatchedAV != null)
            {
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(RavenDefOf.Raven_Thought_WatchedAV);
            }
            // 1. 施加温和感官 Hediff (香炉同款)
            if (Rand.Chance(0.20f))
            {
                HediffDef aura = DefDatabase<HediffDef>.GetNamedSilentFail("RavenHediff_IncenseAura");
                if (aura != null)
                {
                    HealthUtility.AdjustSeverity(pawn, aura, 0.15f);
                    // 【1.6 核心】脏化渲染树，触发实时表情更新
                    pawn.Drawer?.renderer?.SetAllGraphicsDirty();
                }
            }

            // 2. 尝试触发随机交配行为
            if (Rand.Chance(RavenRaceMod.Settings.avMatingChance)) TryTriggerMating(pawn);
        }

        private void TryTriggerMating(Pawn p1)
        {
            if (IsBusyLovin(p1) || p1.Downed || p1.Drafted) return;
            var others = CurrentWatchers.Where(x => x != p1 && !IsBusyLovin(x) && !x.Downed && !x.Drafted).ToList();
            if (others.Count == 0) return;

            Pawn p2 = others.RandomElement();
            Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_ForceLovin, p2);
            p1.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            FleckMaker.ThrowMetaIcon(p1.Position, p1.Map, FleckDefOf.Heart);
        }

        private bool IsBusyLovin(Pawn p)
        {
            if (p.CurJob == null) return false;
            return p.CurJob.def == JobDefOf.Lovin || p.CurJob.def == RavenDefOf.Raven_Job_ForceLovin;
        }
    }
}