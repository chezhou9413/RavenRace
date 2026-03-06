using RimWorld;
using Verse;
using Verse.AI;

namespace RavenRace.Features.ReproductionRequest
{
    [DefOf]
    public static class ReproductionRequestDefOf
    {
        public static DutyDef Raven_Duty_ReproductionRequest_Wait;
        public static DutyDef Raven_Duty_ReproductionRequest_Lovin;
        public static JobDef Raven_Job_ReproductionRequestLovin;
        public static HediffDef Raven_Hediff_SqueezedDry;
        public static ThoughtDef Raven_Thought_SqueezedByGroup;
        public static ThoughtDef Raven_Thought_GroupLovinParticipant;

        public static JobDef Raven_Job_NegotiateWithLeader;

        static ReproductionRequestDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ReproductionRequestDefOf));
        }
    }
}