using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using RavenRace.Features.Bloodline;
using RavenRace.Features.Reproduction;
using RavenRace.Features.RavenRite.Rite_Promotion.Purification.Comps; // [新增引用] 引入纯化系统命名空间

namespace RavenRace.Features.BloodlineRitual
{
    public class JobDriver_BloodlineRitual : JobDriver
    {
        private const int RitualDuration = 5000;
        private const TargetIndex AltarInd = TargetIndex.A;

        protected Building_Cradle Altar => (Building_Cradle)job.GetTarget(AltarInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Altar, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(AltarInd);
            this.FailOn(() => Altar.GetDirectlyHeldThings().Count == 0);

            yield return Toils_Goto.GotoThing(AltarInd, PathEndMode.InteractionCell);

            Toil ritual = ToilMaker.MakeToil("DoRitual");
            ritual.defaultCompleteMode = ToilCompleteMode.Delay;
            ritual.defaultDuration = RitualDuration;
            ritual.WithProgressBar(AltarInd, () => 1f - (float)ritual.actor.jobs.curDriver.ticksLeftThisToil / RitualDuration);

            ritual.initAction = () =>
            {
                SoundDefOf.PsychicPulseGlobal.PlayOneShot(pawn);
            };

            ritual.tickAction = () =>
            {
                pawn.rotationTracker.FaceTarget(Altar);
                if (pawn.IsHashIntervalTick(100))
                {
                    FleckMaker.ThrowLightningGlow(Altar.TrueCenter(), pawn.Map, 1.5f);
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect);
                }
            };

            yield return ritual;

            yield return new Toil
            {
                initAction = () =>
                {
                    CompleteRitual();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private void CompleteRitual()
        {
            if (Altar.GetDirectlyHeldThings().Count == 0) return;

            Thing eggThing = Altar.GetDirectlyHeldThings()[0];
            CompSpiritEgg eggComp = eggThing.TryGetComp<CompSpiritEgg>();

            if (eggComp != null)
            {
                AbsorbBloodline(pawn, eggComp);
            }

            eggThing.Destroy(DestroyMode.Vanish);
            FleckMaker.ThrowLightningGlow(pawn.TrueCenter(), pawn.Map, 3.0f);
            Messages.Message("RavenRace_Ritual_AbsorptionSuccess".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent);
        }

        /// <summary>
        /// 吸收灵卵精髓的核心算法。
        /// [核心修改] 适应解耦架构，杂交成分由血脉组件处理，金乌浓度提升由纯化组件处理。
        /// </summary>
        private void AbsorbBloodline(Pawn invoker, CompSpiritEgg egg)
        {
            CompBloodline invokerBlood = invoker.TryGetComp<CompBloodline>();
            // [新增] 获取角色的纯化组件
            CompPurification invokerPur = invoker.TryGetComp<CompPurification>();

            // 1. 吸收金乌浓度精华 (受限于当前阶段上限)
            if (invokerPur != null)
            {
                float gain = egg.goldenCrowConcentration * 0.2f;
                // 仪式吸收的基底极限设为 1.0f，意味着它可以一直提升浓度直到当前阶段(Stage)的物理上限。
                invokerPur.TryAddGoldenCrowConcentration(gain, 1.0f);
            }

            // 2. 合并杂交血脉成分 (原逻辑保留，依然处理成分融合)
            if (invokerBlood == null) return;

            Dictionary<string, float> newComposition = new Dictionary<string, float>();
            HashSet<string> allKeys = new HashSet<string>();

            if (invokerBlood.BloodlineComposition != null)
                foreach (var k in invokerBlood.BloodlineComposition.Keys) allKeys.Add(k);
            if (egg.bloodlineComposition != null)
                foreach (var k in egg.bloodlineComposition.Keys) allKeys.Add(k);

            foreach (string key in allKeys)
            {
                float valInvoker = invokerBlood.BloodlineComposition.ContainsKey(key) ? invokerBlood.BloodlineComposition[key] : 0f;
                float valEgg = egg.bloodlineComposition.ContainsKey(key) ? egg.bloodlineComposition[key] : 0f;
                // 继承公式：自身占 80%，吞噬蛋占 20%
                float finalVal = (valInvoker * 0.8f) + (valEgg * 0.2f);
                if (finalVal > 0f) newComposition[key] = finalVal;
            }

            invokerBlood.SetBloodlineComposition(newComposition);

            // 确保渡鸦主成分不低于 50%
            if (invokerBlood.BloodlineComposition.ContainsKey("Raven_Race"))
            {
                if (invokerBlood.BloodlineComposition["Raven_Race"] < 0.5f)
                {
                    float raven = 0.5f;
                    float remaining = 0.5f;
                    float otherSum = 0f;
                    foreach (var k in new List<string>(invokerBlood.BloodlineComposition.Keys))
                    {
                        if (k != "Raven_Race") otherSum += invokerBlood.BloodlineComposition[k];
                    }
                    if (otherSum > 0)
                    {
                        foreach (var k in new List<string>(invokerBlood.BloodlineComposition.Keys))
                        {
                            if (k != "Raven_Race")
                                invokerBlood.BloodlineComposition[k] = (invokerBlood.BloodlineComposition[k] / otherSum) * remaining;
                        }
                    }
                    invokerBlood.BloodlineComposition["Raven_Race"] = raven;
                }
            }
            invokerBlood.RefreshAbilities();
        }
    }
}