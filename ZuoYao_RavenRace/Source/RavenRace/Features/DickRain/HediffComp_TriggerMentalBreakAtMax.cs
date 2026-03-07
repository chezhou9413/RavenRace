using RimWorld;
using Verse;

namespace RavenRace.Features.DickRain
{
    public class HediffCompProperties_TriggerMentalBreakAtMax : HediffCompProperties
    {
        public float triggerSeverity = 0.99f;
        public MentalStateDef mentalStateDef;

        public HediffCompProperties_TriggerMentalBreakAtMax()
        {
            compClass = typeof(HediffComp_TriggerMentalBreakAtMax);
        }
    }

    public class HediffComp_TriggerMentalBreakAtMax : HediffComp
    {
        private HediffCompProperties_TriggerMentalBreakAtMax Props =>
            (HediffCompProperties_TriggerMentalBreakAtMax)props;

        private bool _triggered;
        private int _scheduledTick = -1;

        private static int _lastGlobalTick = -1;
        private static int _triggeredThisTick = 0;
        private const int MaxPerTick = 1;
        private const int StaggerInterval = 120;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (_triggered) return;
            if (parent.Severity < Props.triggerSeverity) return;

            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned) return;
            if (!pawn.RaceProps.Humanlike) return;
            if (pawn.Downed) return;
            if (!pawn.Awake()) return;

            MentalStateDef stateDef = Props.mentalStateDef ?? MentalStateDefOf.Berserk;
            if (!stateDef.Worker.StateCanOccur(pawn)) return;
            if (pawn.MentalStateDef != null) return;

            int currentTick = Find.TickManager.TicksGame;

            if (_scheduledTick < 0)
            {
                int slot = pawn.thingIDNumber % StaggerInterval;
                _scheduledTick = currentTick + slot;
            }

            if (currentTick < _scheduledTick) return;

            if (_lastGlobalTick != currentTick)
            {
                _lastGlobalTick = currentTick;
                _triggeredThisTick = 0;
            }

            if (_triggeredThisTick >= MaxPerTick)
            {
                _scheduledTick = currentTick + StaggerInterval;
                return;
            }

            _triggeredThisTick++;
            _triggered = true;

            pawn.mindState.mentalStateHandler.TryStartMentalState(
                stateDef,
                reason: "DickRain_Arousal",
                forced: true,
                forceWake: true
            );

            Find.LetterStack.ReceiveLetter(
                "性欲失控",
                $"{pawn.NameShortColored} 在迪克雨的影响下彻底失控了！",
                LetterDefOf.ThreatSmall,
                pawn
            );

            parent.pawn.health.hediffSet.hediffs.Remove(parent);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref _triggered, "triggered", false);
            Scribe_Values.Look(ref _scheduledTick, "scheduledTick", -1);
        }
    }
}