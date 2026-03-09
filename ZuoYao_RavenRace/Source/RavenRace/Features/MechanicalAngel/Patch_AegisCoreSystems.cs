using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RavenRace.Features.MechanicalAngel
{
    [StaticConstructorOnStartup] // 确保在游戏启动时缓存Def
    [HarmonyPatch]
    public static class Patch_AegisCoreSystems
    {
        // 【核心修复】缓存 Def 引用，避免每次都用 GetNamed
        private static readonly NeedDef MechEnergyDef;
        private static readonly NeedDef AegisLustDef;
        private static readonly HediffDef AegisPanaceaDef;
        private static readonly ThoughtDef AegisPanaceaThoughtDef;
        private static readonly HediffDef AegisRampageDef;
        private static readonly JobDef AegisRampageJobDef;
        private static readonly SoundDef AegisTreatmentSoundDef;

        static Patch_AegisCoreSystems()
        {
            MechEnergyDef = DefDatabase<NeedDef>.GetNamed("MechEnergy");
            AegisLustDef = DefDatabase<NeedDef>.GetNamed("Raven_Need_AegisLust");
            AegisPanaceaDef = DefDatabase<HediffDef>.GetNamed("Raven_Hediff_AegisPanacea");
            AegisPanaceaThoughtDef = DefDatabase<ThoughtDef>.GetNamed("Raven_Thought_AegisPanacea");
            AegisRampageDef = DefDatabase<HediffDef>.GetNamed("Raven_Hediff_AegisRampage");
            AegisRampageJobDef = DefDatabase<JobDef>.GetNamed("Raven_Job_AegisRampageCharge");
            AegisTreatmentSoundDef = DefDatabase<SoundDef>.GetNamed("RavenMechAegis_PaPaPa"); // 使用您指定的新音效名
        }

        // 补丁1: 替换能量系统
        [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
        [HarmonyPostfix]
        public static void ShouldHaveNeed_Postfix(Pawn ___pawn, NeedDef nd, ref bool __result)
        {
            if (___pawn.def != RavenDefOf.Raven_Mech_Aegis) return;

            // 【核心修复】使用缓存的 Def 进行比较
            if (nd == MechEnergyDef)
            {
                __result = false;
            }
            else if (nd == AegisLustDef)
            {
                __result = true;
            }
        }

        // 补丁2: 解锁装备面板
        [HarmonyPatch(typeof(ITab_Pawn_Gear), "IsVisible", MethodType.Getter)]
        [HarmonyPostfix]
        public static void IsVisible_Postfix(ITab_Pawn_Gear __instance, ref bool __result)
        {
            Pawn pawn = Traverse.Create(__instance).Property("SelPawnForGear").GetValue<Pawn>();
            if (!__result && pawn != null && pawn.def == RavenDefOf.Raven_Mech_Aegis)
            {
                __result = true;
            }
        }

        // 补丁3: 拦截治疗行为
        [HarmonyPatch(typeof(TendUtility), "DoTend")]
        [HarmonyPrefix]
        public static bool DoTend_Prefix(Pawn doctor, Pawn patient)
        {
            if (doctor == null || doctor.def != RavenDefOf.Raven_Mech_Aegis) return true;

            var lustNeed = doctor.needs.TryGetNeed<Need_AegisLust>();
            if (lustNeed == null || lustNeed.CurLevel < 10f)
            {
                Messages.Message("艾吉斯淫能不足，无法进行神圣治疗。", doctor, MessageTypeDefOf.RejectInput);
                return false;
            }
            lustNeed.CurLevel -= 10f;
            float quality = 2.0f;

            var hediffsToTend = new System.Collections.Generic.List<Hediff>();
            TendUtility.GetOptimalHediffsToTendWithSingleTreatment(patient, true, hediffsToTend, null);

            foreach (var hediff in hediffsToTend)
            {
                hediff.Tended(quality, quality, 0);
            }

            patient.health.AddHediff(AegisPanaceaDef);
            patient.needs.mood?.thoughts.memories.TryGainMemory(AegisPanaceaThoughtDef, doctor);

            FleckMaker.ThrowMetaIcon(patient.Position, patient.Map, FleckDefOf.Heart);
            AegisTreatmentSoundDef?.PlayOneShot(new TargetInfo(patient.Position, patient.Map));

            patient.records.Increment(RecordDefOf.TimesTendedTo);
            doctor.records.Increment(RecordDefOf.TimesTendedOther);

            return false;
        }

        // 补丁4 & 5: 拦截手术失败判定与成功后的音效
        [HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail")]
        [HarmonyPrefix]
        public static bool CheckSurgeryFail_Prefix(ref bool __result, Pawn surgeon)
        {
            if (surgeon != null && surgeon.def == RavenDefOf.Raven_Mech_Aegis)
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Recipe_Surgery), "OnSurgerySuccess")]
        [HarmonyPostfix]
        public static void OnSurgerySuccess_Postfix(Pawn pawn, Pawn billDoer)
        {
            if (billDoer != null && billDoer.def == RavenDefOf.Raven_Mech_Aegis)
            {
                var lustNeed = billDoer.needs.TryGetNeed<Need_AegisLust>();
                if (lustNeed != null && lustNeed.CurLevel >= 10f)
                {
                    lustNeed.CurLevel -= 10f;
                    pawn.health.AddHediff(AegisPanaceaDef);
                    pawn.needs.mood?.thoughts.memories.TryGainMemory(AegisPanaceaThoughtDef, billDoer);
                    AegisTreatmentSoundDef?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                }
            }
        }

        // 补丁6: 拦截AI决策
        [HarmonyPatch(typeof(Pawn_JobTracker), "DetermineNextJob")]
        [HarmonyPrefix]
        public static bool DetermineNextJob_Prefix(ref ThinkResult __result, Pawn ___pawn)
        {
            var coreComp = ___pawn.GetComp<CompAegisCore>();
            if (coreComp != null && coreComp.isRampaging)
            {
                Pawn target = (Pawn)GenClosest.ClosestThing_Global(___pawn.Position, ___pawn.Map.mapPawns.AllPawnsSpawned, 9999f,
                    t => t is Pawn p && p != ___pawn && !p.Dead && p.def != RavenDefOf.Raven_Mech_Aegis && ___pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly));

                if (target != null)
                {
                    Job job = JobMaker.MakeJob(AegisRampageJobDef, target);
                    __result = new ThinkResult(job, null, JobTag.Misc, false);
                    return false;
                }
            }
            return true;
        }

        // 补丁7: 拦截机械族列表的能量条绘制
        [HarmonyPatch(typeof(PawnColumnWorker_Energy), "DoCell")]
        [HarmonyPrefix]
        public static bool DoCell_Prefix(Rect rect, Pawn pawn)
        {
            if (pawn.def != RavenDefOf.Raven_Mech_Aegis)
            {
                return true;
            }

            Need_AegisLust lustNeed = pawn.needs.TryGetNeed<Need_AegisLust>();
            if (lustNeed == null) return false;

            Rect barRect = rect.ContractedBy(4f);
            Widgets.FillableBar(barRect, lustNeed.CurLevelPercentage, SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.41f, 0.7f)), BaseContent.ClearTex, false);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, Mathf.RoundToInt(lustNeed.CurLevel).ToString() + " / " + Mathf.RoundToInt(lustNeed.MaxLevel).ToString());
            Text.Anchor = TextAnchor.UpperLeft;

            return false;
        }
    }
}