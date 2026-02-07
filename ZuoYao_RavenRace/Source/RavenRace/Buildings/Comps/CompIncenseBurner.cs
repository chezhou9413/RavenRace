using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.AI;
using System.Linq;

namespace RavenRace.Buildings.Comps
{
    public class CompProperties_IncenseBurner : CompProperties
    {
        public float effectRadius = 9.9f;
        public float joyAmount = 0.05f;
        public float forceLovinChance = 0.05f;

        public CompProperties_IncenseBurner()
        {
            this.compClass = typeof(CompIncenseBurner);
        }
    }

    public class CompIncenseBurner : ThingComp
    {
        public CompProperties_IncenseBurner Props => (CompProperties_IncenseBurner)props;
        private CompRefuelable refuelable;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelable = parent.GetComp<CompRefuelable>();
        }

        public override void CompTick()
        {
            base.CompTick();
            // 使用设置中的间隔
            if (parent.IsHashIntervalTick(RavenRaceMod.Settings.incenseCheckInterval))
            {
                DoEffect();
            }
        }

        private void DoEffect()
        {
            if (refuelable != null && !refuelable.HasFuel) return;

            Map map = parent.Map;
            List<Pawn> pawnsInRange = new List<Pawn>();

            // 1. 获取范围内生物
            foreach (Thing t in GenRadial.RadialDistinctThingsAround(parent.Position, map, Props.effectRadius, true))
            {
                if (t is Pawn p && !p.Dead && !p.Downed && p.RaceProps.Humanlike)
                {
                    pawnsInRange.Add(p);
                }
            }

            if (pawnsInRange.Count == 0) return;

            foreach (Pawn p in pawnsInRange)
            {
                // A. 娱乐与心情
                p.needs?.joy?.GainJoy(RavenRaceMod.Settings.incenseJoyAmount, JoyKindDefOf.Social);

                if (RavenBuildingDefOf.Raven_Thought_IncenseSmell != null)
                {
                    p.needs?.mood?.thoughts?.memories?.TryGainMemory(RavenBuildingDefOf.Raven_Thought_IncenseSmell);
                }

                // B. [修改] 施加独立的香炉氛围 Hediff (无副作用)
                HediffDef auraDef = DefDatabase<HediffDef>.GetNamedSilentFail("RavenHediff_IncenseAura");
                if (auraDef != null)
                {
                    HealthUtility.AdjustSeverity(p, auraDef, 0.01f);
                }
            }

            // 2. 触发技能
            TriggerRavenAbility(pawnsInRange);
        }

        private void TriggerRavenAbility(List<Pawn> pawnsInRange)
        {
            List<Pawn> capableRavens = pawnsInRange
                .Where(p => p.def.defName == "Raven_Race" && !p.Drafted &&
                            p.abilities?.GetAbility(RavenDefOf.Raven_Ability_ForceLovin) != null)
                .ToList();

            if (capableRavens.Count > 0 && Rand.Chance(RavenRaceMod.Settings.incenseForceLovinChance))
            {
                Pawn caster = capableRavens.RandomElement();

                // [修复] 检查施法者是否已经在忙于交配
                if (IsBusyLovin(caster)) return;

                var validTargets = pawnsInRange.Where(t => t != caster && !t.Downed && !IsBusyLovin(t)).ToList();
                if (validTargets.Count == 0) return;

                Pawn target = validTargets.FirstOrDefault(t => LovePartnerRelationUtility.LovePartnerRelationExists(caster, t));
                if (target == null)
                {
                    target = validTargets.Where(t => t.gender != caster.gender && caster.relations.OpinionOf(t) > 20).RandomElementWithFallback();
                }
                if (target == null) target = validTargets.RandomElement();

                if (target != null)
                {
                    Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_ForceLovin, target);
                    caster.jobs.TryTakeOrderedJob(job, JobTag.Misc);

                    FleckMaker.ThrowMetaIcon(caster.Position, caster.Map, FleckDefOf.Heart);
                    Messages.Message("Raven_Message_IncenseEffect".Translate(caster.LabelShort, target.LabelShort), caster, MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        // [新增] 检查是否正在进行交配任务
        private bool IsBusyLovin(Pawn p)
        {
            if (p.CurJob == null) return false;
            return p.CurJob.def == JobDefOf.Lovin ||
                   p.CurJob.def == RavenDefOf.Raven_Job_ForceLovin;
        }
    }
}