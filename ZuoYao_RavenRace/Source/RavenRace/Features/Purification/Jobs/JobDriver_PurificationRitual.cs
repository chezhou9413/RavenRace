using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RavenRace.Features.Bloodline;

namespace RavenRace.Features.Purification
{
    /// <summary>
    /// 执行纯化仪式的动作逻辑。
    /// </summary>
    public class JobDriver_PurificationRitual : JobDriver
    {
        private const TargetIndex AltarInd = TargetIndex.A;

        protected Building Altar => (Building)job.GetTarget(AltarInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Altar, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(AltarInd);

            yield return Toils_Goto.GotoThing(AltarInd, PathEndMode.InteractionCell);

            Toil ritualToil = ToilMaker.MakeToil("DoPurification");
            // 读取设置中的时长
            int duration = RavenRaceMod.Settings.purificationRitualDurationTicks;

            ritualToil.defaultCompleteMode = ToilCompleteMode.Delay;
            ritualToil.defaultDuration = duration;
            ritualToil.WithProgressBarToilDelay(AltarInd);

            ritualToil.initAction = () =>
            {
                SoundDefOf.MechSerumUsed.PlayOneShot(pawn);
            };

            ritualToil.tickAction = () =>
            {
                pawn.rotationTracker.FaceTarget(Altar);
                // 周期性扔特效
                if (pawn.IsHashIntervalTick(60))
                {
                    FleckMaker.ThrowLightningGlow(Altar.TrueCenter(), pawn.Map, 1.5f);
                    FleckMaker.ThrowMicroSparks(pawn.Position.ToVector3Shifted(), pawn.Map);
                }
            };

            yield return ritualToil;

            // 结算阶段
            yield return new Toil
            {
                initAction = () =>
                {
                    ResolveRitual();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private void ResolveRitual()
        {
            // [核心修复] 改为获取全新的金乌纯化组件，而非杂交血脉组件
            var comp = pawn.TryGetComp<CompPurification>();
            if (comp == null) return;

            float successChance = RavenRaceMod.Settings.purificationSuccessChance;

            if (Rand.Chance(successChance))
            {
                // 【成功】
                // 1. 突破阶段
                comp.currentPurificationStage++;

                // 2. 强制赋予奖励浓度 (这里因为突破了，所以上限自动提高了，调用 TryAdd 时用极大的 sourceMaxLimit)
                comp.TryAddGoldenCrowConcentration(0.01f, 1.0f);

                // 3. 视觉反馈
                FleckMaker.ThrowExplosionCell(pawn.Position, pawn.Map, FleckDefOf.ExplosionFlash, Color.yellow);
                SoundDefOf.PsychicPulseGlobal.PlayOneShot(pawn);

                Find.LetterStack.ReceiveLetter(
                    "RavenRace_LetterLabel_PurificationSuccess".Translate(), // "纯化成功！"
                    $"经过艰苦的试炼，{pawn.LabelShort} 体内的金乌血脉打破了桎梏，成功迈入了阶段 {comp.currentPurificationStage}！\n\n其浓度上限已解锁，并额外获得了 1% 的纯净浓度奖励。",
                    LetterDefOf.PositiveEvent,
                    pawn
                );
            }
            else
            {
                // 【失败】
                FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, 2f);

                Find.LetterStack.ReceiveLetter(
                    "RavenRace_LetterLabel_PurificationFailed".Translate(), // "纯化失败"
                    $"仪式结束了。基座的能量耗尽，但 {pawn.LabelShort} 体内的血脉并未产生质变。除了虚脱，什么也没有发生。",
                    LetterDefOf.NegativeEvent,
                    pawn
                );
            }
        }
    }
}