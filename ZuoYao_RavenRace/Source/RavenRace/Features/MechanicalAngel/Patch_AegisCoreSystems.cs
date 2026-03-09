using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RavenRace.Features.MechanicalAngel
{
    [HarmonyPatch]
    public static class Patch_AegisCoreSystems
    {
        // =========================================================
        // 1. 无痛伪装原版能量条 (让维修自动扣除淫能)
        // 修复：使用 Harmony 的 ___pawn 参数安全访问受保护字段
        // =========================================================

        [HarmonyPatch(typeof(Need), "get_LabelCap")]
        [HarmonyPostfix]
        public static void LabelCap_Postfix(Need __instance, Pawn ___pawn, ref string __result)
        {
            if (__instance is Need_MechEnergy && ___pawn?.def == RavenDefOf.Raven_Mech_Aegis)
            {
                __result = "<color=#FF69B4>淫能</color>";
            }
        }

        [HarmonyPatch(typeof(Need_MechEnergy), "GetTipString")]
        [HarmonyPostfix]
        public static void GetTipString_Postfix(Need_MechEnergy __instance, Pawn ___pawn, ref string __result)
        {
            if (___pawn?.def == RavenDefOf.Raven_Mech_Aegis)
            {
                __result = "<color=#FF69B4>淫能: " + __instance.CurLevelPercentage.ToStringPercent() + "</color>\n" +
                           "艾吉斯核心特有的能量系统。必须通过与生命体发生剧烈互动来汲取精气充能。\n(技师在维修她时，也会大量消耗此能量)\n\n" +
                           "当前每天自然流失: " + (__instance.FallPerDay / 100f).ToStringPercent();
            }
        }

        [HarmonyPatch(typeof(Need_MechEnergy), "get_MaxLevel")]
        [HarmonyPrefix]
        public static bool MaxLevel_Prefix(Need_MechEnergy __instance, Pawn ___pawn, ref float __result)
        {
            if (___pawn?.def == RavenDefOf.Raven_Mech_Aegis)
            {
                __result = 100f; // 锁定最大值为100
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Need_MechEnergy), "get_FallPerDay")]
        [HarmonyPrefix]
        public static bool FallPerDay_Prefix(Need_MechEnergy __instance, Pawn ___pawn, ref float __result)
        {
            Pawn p = ___pawn;
            if (p != null && p.def == RavenDefOf.Raven_Mech_Aegis)
            {
                if (p.Downed || !p.Awake()) { __result = 0f; return false; }
                if (p.CurJobDef == RavenDefOf.Raven_Job_AegisLustCharge || p.CurJobDef == RavenDefOf.Raven_Job_AegisRampageCharge)
                {
                    __result = -100f; // 正在做爱时大幅度恢复
                    return false;
                }
                __result = (p.mindState != null && !p.mindState.IsIdle) ? 20f : 5f;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Need), "DrawOnGUI")]
        [HarmonyPrefix]
        public static void DrawOnGUI_Prefix(Need __instance, Pawn ___pawn, out Color __state)
        {
            __state = GUI.color;
            if (__instance is Need_MechEnergy && ___pawn?.def == RavenDefOf.Raven_Mech_Aegis)
            {
                GUI.color = new Color(1f, 0.41f, 0.7f, 1f); // 画条时强制染成粉色
            }
        }

        [HarmonyPatch(typeof(Need), "DrawOnGUI")]
        [HarmonyPostfix]
        public static void DrawOnGUI_Postfix(Need __instance, Color __state)
        {
            GUI.color = __state;
        }

        [HarmonyPatch(typeof(PawnColumnWorker_Energy), "DoCell")]
        [HarmonyPrefix]
        public static bool PawnColumnWorker_Prefix(Rect rect, Pawn pawn)
        {
            if (pawn.def == RavenDefOf.Raven_Mech_Aegis && !pawn.IsGestating() && pawn.needs?.energy != null)
            {
                Widgets.FillableBar(rect.ContractedBy(4f), pawn.needs.energy.CurLevelPercentage, SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.41f, 0.7f)), BaseContent.ClearTex, false);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, Mathf.RoundToInt(pawn.needs.energy.CurLevel).ToString() + " / " + Mathf.RoundToInt(pawn.needs.energy.MaxLevel).ToString());
                Text.Anchor = TextAnchor.UpperLeft;
                return false;
            }
            return true;
        }

        // =========================================================
        // 2. 彻底屏蔽调教仓的有毒垃圾产出和空指针报错
        // =========================================================

        [HarmonyPatch(typeof(CompWasteProducer), "ProduceWaste")]
        [HarmonyPrefix]
        public static bool ProduceWaste_Prefix(CompWasteProducer __instance)
        {
            // 如果是我们的调教仓，拦截产出方法，不产生任何垃圾！
            if (__instance.parent.def == RavenDefOf.Raven_Building_AngelGestator) return false;
            return true;
        }

        [HarmonyPatch(typeof(CompWasteProducer), "get_CanEmptyNow")]
        [HarmonyPostfix]
        public static void CanEmptyNow_Postfix(CompWasteProducer __instance, ref bool __result)
        {
            // 告诉AI，我们的仓不需要清空垃圾
            if (__instance.parent.def == RavenDefOf.Raven_Building_AngelGestator) __result = false;
        }

        [HarmonyPatch(typeof(CompWasteProducer), "get_Waste")]
        [HarmonyPrefix]
        public static bool GetWaste_Prefix(CompWasteProducer __instance, ref Thing __result)
        {
            // 拦截核心空指针来源：如果内部容器为空或者是我们的仓，直接返回null
            if (__instance.parent.def == RavenDefOf.Raven_Building_AngelGestator || __instance.parent.TryGetInnerInteractableThingOwner() == null)
            {
                __result = null;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CompWasteProducer), "CompInspectStringExtra")]
        [HarmonyPrefix]
        public static bool CompInspectStringExtra_Prefix(CompWasteProducer __instance, ref string __result)
        {
            // 面板上不显示有毒垃圾
            if (__instance.parent.def == RavenDefOf.Raven_Building_AngelGestator)
            {
                __result = null;
                return false;
            }
            return true;
        }

        // =========================================================
        // 3. 拦截治疗、手术和解锁装备栏
        // =========================================================

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

        [HarmonyPatch(typeof(TendUtility), "DoTend")]
        [HarmonyPrefix]
        public static bool DoTend_Prefix(Pawn doctor, Pawn patient)
        {
            if (doctor == null || doctor.def != RavenDefOf.Raven_Mech_Aegis) return true;

            var lustNeed = doctor.needs.energy;
            if (lustNeed == null || lustNeed.CurLevel < 10f)
            {
                Messages.Message("艾吉斯淫能不足，无法进行神圣治疗。", doctor, MessageTypeDefOf.RejectInput);
                return false;
            }
            lustNeed.CurLevel -= 10f;
            float quality = 2.0f; // 固定200%治疗质量

            var hediffsToTend = new System.Collections.Generic.List<Hediff>();
            TendUtility.GetOptimalHediffsToTendWithSingleTreatment(patient, true, hediffsToTend, null);

            foreach (var hediff in hediffsToTend) hediff.Tended(quality, quality, 0);

            if (RavenDefOf.Raven_Hediff_AegisPanacea != null)
                patient.health.AddHediff(RavenDefOf.Raven_Hediff_AegisPanacea);

            if (RavenDefOf.Raven_Thought_AegisPanacea != null && patient.needs?.mood != null)
                patient.needs.mood.thoughts.memories.TryGainMemory(RavenDefOf.Raven_Thought_AegisPanacea, doctor);

            FleckMaker.ThrowMetaIcon(patient.Position, patient.Map, FleckDefOf.Heart);
            SoundDef treatmentSound = DefDatabase<SoundDef>.GetNamedSilentFail("RavenMechAegis_PaPaPa");
            treatmentSound?.PlayOneShot(new TargetInfo(patient.Position, patient.Map));

            patient.records.Increment(RecordDefOf.TimesTendedTo);
            doctor.records.Increment(RecordDefOf.TimesTendedOther);

            return false;
        }

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
                var lustNeed = billDoer.needs.energy;
                if (lustNeed != null && lustNeed.CurLevel >= 10f)
                {
                    lustNeed.CurLevel -= 10f;
                    if (RavenDefOf.Raven_Hediff_AegisPanacea != null)
                        pawn.health.AddHediff(RavenDefOf.Raven_Hediff_AegisPanacea);

                    if (RavenDefOf.Raven_Thought_AegisPanacea != null && pawn.needs?.mood != null)
                        pawn.needs.mood.thoughts.memories.TryGainMemory(RavenDefOf.Raven_Thought_AegisPanacea, billDoer);

                    SoundDef treatmentSound = DefDatabase<SoundDef>.GetNamedSilentFail("RavenMechAegis_PaPaPa");
                    treatmentSound?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_JobTracker), "DetermineNextJob")]
        [HarmonyPrefix]
        public static bool DetermineNextJob_Prefix(ref ThinkResult __result, Pawn ___pawn)
        {
            var coreComp = ___pawn.GetComp<CompAegisCore>();
            if (coreComp != null && coreComp.isRampaging)
            {
                Pawn target = (Pawn)GenClosest.ClosestThing_Global(___pawn.Position, ___pawn.Map.mapPawns.AllPawnsSpawned, 9999f,
                    t => t is Pawn p && p != ___pawn && !p.Dead && p.def != RavenDefOf.Raven_Mech_Aegis && ___pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly));

                if (target != null && RavenDefOf.Raven_Job_AegisRampageCharge != null)
                {
                    Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_AegisRampageCharge, target);
                    __result = new ThinkResult(job, null, JobTag.Misc, false);
                    return false;
                }
            }
            return true;
        }
    }
}