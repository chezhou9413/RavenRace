using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RavenRace.Features.MechanicalAngel
{
    /// <summary>
    /// 艾吉斯核心系统拦截补丁综合类。
    /// 职责：
    /// 1. 伪装原版能量条为淫能。
    /// 2. 屏蔽有毒垃圾产出。
    /// 3. 拦截医疗和手术逻辑（音效替换与极乐结算）。
    /// 4. 绕过原版培育仓的机械师带宽限制。
    /// </summary>
    [HarmonyPatch]
    public static class Patch_AegisCoreSystems
    {
        private static Pawn GetPawn(Need need)
        {
            return Traverse.Create(need).Field("pawn").GetValue<Pawn>();
        }

        private static bool IsAegis(Pawn pawn)
        {
            return pawn != null && pawn.def == RavenDefOf.Raven_Mech_Aegis;
        }

        // =========================================================
        // 1. 无痛伪装原版能量条
        // =========================================================
        [HarmonyPatch(typeof(Need), "get_LabelCap")]
        [HarmonyPostfix]
        public static void LabelCap_Postfix(Need __instance, ref string __result)
        {
            if (__instance is Need_MechEnergy && IsAegis(GetPawn(__instance)))
            {
                __result = "<color=#FF69B4>淫能</color>";
            }
        }

        [HarmonyPatch(typeof(Need_MechEnergy), "GetTipString")]
        [HarmonyPostfix]
        public static void GetTipString_Postfix(Need_MechEnergy __instance, ref string __result)
        {
            if (IsAegis(GetPawn(__instance)))
            {
                __result = "<color=#FF69B4>淫能: " + __instance.CurLevelPercentage.ToStringPercent() + "</color>\n" +
                           "艾吉斯核心特有的能量系统。必须通过与生命体发生剧烈互动来汲取精气充能。\n(技师在维修她时，也会大量消耗此能量)\n\n" +
                           "当前每天自然流失: " + (__instance.FallPerDay / 100f).ToStringPercent();
            }
        }

        [HarmonyPatch(typeof(Need_MechEnergy), "get_MaxLevel")]
        [HarmonyPrefix]
        public static bool MaxLevel_Prefix(Need_MechEnergy __instance, ref float __result)
        {
            if (IsAegis(GetPawn(__instance)))
            {
                __result = 100f;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Need_MechEnergy), "get_FallPerDay")]
        [HarmonyPrefix]
        public static bool FallPerDay_Prefix(Need_MechEnergy __instance, ref float __result)
        {
            Pawn pawn = GetPawn(__instance);
            if (IsAegis(pawn))
            {
                if (pawn.Downed || !pawn.Awake()) { __result = 0f; return false; }
                if (pawn.CurJobDef == RavenDefOf.Raven_Job_AegisLustCharge || pawn.CurJobDef == RavenDefOf.Raven_Job_AegisRampageCharge)
                {
                    __result = -100f; // 榨汁时大幅度恢复
                    return false;
                }
                __result = (pawn.mindState != null && !pawn.mindState.IsIdle) ? 20f : 5f;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Need), "DrawOnGUI")]
        [HarmonyPrefix]
        public static void DrawOnGUI_Prefix(Need __instance, out Color __state)
        {
            __state = GUI.color;
            if (__instance is Need_MechEnergy && IsAegis(GetPawn(__instance)))
            {
                GUI.color = new Color(1f, 0.41f, 0.7f, 1f);
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
            if (IsAegis(pawn) && !pawn.IsGestating() && pawn.needs?.energy != null)
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
        public static bool ProduceWaste_Prefix(CompWasteProducer __instance) => __instance.parent.def != RavenDefOf.Raven_Building_AngelGestator;

        [HarmonyPatch(typeof(CompWasteProducer), "get_CanEmptyNow")]
        [HarmonyPostfix]
        public static void CanEmptyNow_Postfix(CompWasteProducer __instance, ref bool __result)
        {
            if (__instance.parent.def == RavenDefOf.Raven_Building_AngelGestator) __result = false;
        }

        [HarmonyPatch(typeof(CompWasteProducer), "get_Waste")]
        [HarmonyPrefix]
        public static bool GetWaste_Prefix(CompWasteProducer __instance, ref Thing __result)
        {
            if (__instance.parent.def == RavenDefOf.Raven_Building_AngelGestator || __instance.parent.TryGetInnerInteractableThingOwner() == null)
            {
                __result = null; return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CompWasteProducer), "CompInspectStringExtra")]
        [HarmonyPrefix]
        public static bool CompInspectStringExtra_Prefix(CompWasteProducer __instance, ref string __result)
        {
            if (__instance.parent.def == RavenDefOf.Raven_Building_AngelGestator)
            {
                __result = null; return false;
            }
            return true;
        }

        // =========================================================
        // 3. 拦截医疗、手术、装备和音效 (瞬时替换模式)
        // =========================================================
        [HarmonyPatch(typeof(ITab_Pawn_Gear), "IsVisible", MethodType.Getter)]
        [HarmonyPostfix]
        public static void IsVisible_Postfix(ITab_Pawn_Gear __instance, ref bool __result)
        {
            Pawn pawn = Traverse.Create(__instance).Property("SelPawnForGear").GetValue<Pawn>();
            if (!__result && IsAegis(pawn)) __result = true;
        }

        // 【核心修复】使用闭包动态静音原版治疗和手术音效
        [HarmonyPatch(typeof(ToilEffects), "PlaySustainerOrSound", new Type[] { typeof(Toil), typeof(Func<SoundDef>), typeof(float) })]
        [HarmonyPrefix]
        public static void PlaySustainerOrSound_Prefix(Toil toil, ref Func<SoundDef> soundDefGetter)
        {
            Func<SoundDef> originalGetter = soundDefGetter;
            soundDefGetter = () =>
            {
                Pawn pawn = toil.actor; // 在实际运行的帧数获取真正的操作者
                if (IsAegis(pawn))
                {
                    if (pawn.CurJobDef == JobDefOf.TendPatient ||
                       (pawn.CurJobDef == JobDefOf.DoBill && pawn.CurJob.GetTarget(TargetIndex.A).Thing is Pawn))
                    {
                        // 只要是艾吉斯在做手术/治疗，强制把原版的持续音效变哑巴
                        return null;
                    }
                }
                return originalGetter();
            };
        }

        [HarmonyPatch(typeof(TendUtility), "DoTend")]
        [HarmonyPrefix]
        public static bool DoTend_Prefix(Pawn doctor, Pawn patient)
        {
            if (!IsAegis(doctor)) return true;

            var lustNeed = doctor.needs.energy;
            if (lustNeed == null || lustNeed.CurLevel < 10f)
            {
                Messages.Message("艾吉斯淫能不足，无法进行神圣治疗。", doctor, MessageTypeDefOf.RejectInput);
                return false;
            }
            lustNeed.CurLevel -= 10f;
            float quality = 2.0f;

            var hediffsToTend = new System.Collections.Generic.List<Hediff>();
            TendUtility.GetOptimalHediffsToTendWithSingleTreatment(patient, true, hediffsToTend, null);
            foreach (var hediff in hediffsToTend) hediff.Tended(quality, quality, 0);

            if (RavenDefOf.Raven_Hediff_AegisPanacea != null)
                patient.health.AddHediff(RavenDefOf.Raven_Hediff_AegisPanacea);
            if (RavenDefOf.Raven_Thought_AegisPanacea != null && patient.needs?.mood != null)
                patient.needs.mood.thoughts.memories.TryGainMemory(RavenDefOf.Raven_Thought_AegisPanacea, doctor);

            FleckMaker.ThrowMetaIcon(patient.Position, patient.Map, FleckDefOf.Heart);

            // 【结算】治疗结束瞬间播放 PaPaPa
            SoundDef treatmentSound = DefDatabase<SoundDef>.GetNamedSilentFail("RavenMechAegis_PaPaPa");
            treatmentSound?.PlayOneShot(new TargetInfo(patient.Position, patient.Map));

            patient.records?.Increment(RavenDefOf.Raven_Record_LovinCount);
            doctor.records?.Increment(RavenDefOf.Raven_Record_LovinCount);
            patient.records?.Increment(RecordDefOf.TimesTendedTo);
            doctor.records?.Increment(RecordDefOf.TimesTendedOther);

            return false;
        }

        [HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail")]
        [HarmonyPrefix]
        public static bool CheckSurgeryFail_Prefix(ref bool __result, Pawn surgeon)
        {
            if (IsAegis(surgeon)) { __result = false; return false; }
            return true;
        }

        [HarmonyPatch(typeof(Recipe_Surgery), "OnSurgerySuccess")]
        [HarmonyPostfix]
        public static void OnSurgerySuccess_Postfix(Pawn pawn, Pawn billDoer)
        {
            if (IsAegis(billDoer))
            {
                var lustNeed = billDoer.needs.energy;
                if (lustNeed != null && lustNeed.CurLevel >= 10f)
                {
                    lustNeed.CurLevel -= 10f;
                    if (RavenDefOf.Raven_Hediff_AegisPanacea != null)
                        pawn.health.AddHediff(RavenDefOf.Raven_Hediff_AegisPanacea);
                    if (RavenDefOf.Raven_Thought_AegisPanacea != null && pawn.needs?.mood != null)
                        pawn.needs.mood.thoughts.memories.TryGainMemory(RavenDefOf.Raven_Thought_AegisPanacea, billDoer);

                    // 【结算】手术结束瞬间播放 PaPaPa
                    SoundDef treatmentSound = DefDatabase<SoundDef>.GetNamedSilentFail("RavenMechAegis_PaPaPa");
                    treatmentSound?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

                    pawn.records?.Increment(RavenDefOf.Raven_Record_LovinCount);
                    billDoer.records?.Increment(RavenDefOf.Raven_Record_LovinCount);
                }
            }
        }

        // 暴走索敌拦截
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

        // =========================================================
        // 4. 绕过原版机械师身份和带宽限制，允许任何人操作调教舱
        // =========================================================
        [HarmonyPatch(typeof(Bill_Mech), "PawnAllowedToStartAnew")]
        [HarmonyPrefix]
        public static bool PawnAllowedToStartAnew_Prefix(Bill_Mech __instance, Pawn p, ref bool __result)
        {
            if (__instance.recipe.defName == "Make_Raven_Mech_Aegis")
            {
                if (!ModLister.CheckBiotech("Mech bill")) { __result = false; return false; }
                if (!__instance.recipe.PawnSatisfiesSkillRequirements(p)) { __result = false; return false; }
                if (__instance.BoundPawn != null && __instance.BoundPawn != p)
                {
                    JobFailReason.Is("AlreadyAssigned".Translate() + " (" + __instance.BoundPawn.LabelShort + ")", null);
                    __result = false;
                    return false;
                }
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Bill_Mech), "ShouldDoNow")]
        [HarmonyPrefix]
        public static bool ShouldDoNow_Prefix(Bill_Mech __instance, ref bool __result)
        {
            if (__instance.recipe.defName == "Make_Raven_Mech_Aegis")
            {
                __result = !__instance.suspended;
                return false;
            }
            return true;
        }
    }
}