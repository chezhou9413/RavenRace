using HarmonyLib;
using Verse;
using RimWorld;
using Verse.AI;

namespace RavenRace.Features.UniqueEquipment.ShadowCloak.Harmony
{
    // 补丁1：攻击命中后给予加速和冷却，并强制重置姿态
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class Patch_ApplyMeleeDamageToTarget
    {
        [HarmonyPostfix]
        public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target, DamageWorker.DamageResult __result)
        {
            if (ShadowCloakDefOf.Raven_Apparel_ShadowCloak == null) return;

            Pawn caster = __instance.CasterPawn;
            if (caster == null || caster.Dead) return;

            // 检查是否穿着暗影斗篷
            bool wearing = false;
            if (caster.apparel != null)
            {
                foreach (var ap in caster.apparel.WornApparel)
                    if (ap.def == ShadowCloakDefOf.Raven_Apparel_ShadowCloak) { wearing = true; break; }
            }

            if (wearing)
            {
                // 1. 添加 "暗影冷却" Hediff (禁止攻击，大幅加速)
                Hediff cooldown = caster.health.hediffSet.GetFirstHediffOfDef(ShadowCloakDefOf.Raven_Hediff_ShadowAttackCooldown);
                if (cooldown != null) cooldown.ageTicks = 0;
                else caster.health.AddHediff(ShadowCloakDefOf.Raven_Hediff_ShadowAttackCooldown);

                // 2. 添加/刷新 "暗影步" Hediff (原有的隐身或加速效果，保持兼容)
                if (ShadowCloakDefOf.Raven_Hediff_ShadowStep != null)
                {
                    Hediff step = caster.health.hediffSet.GetFirstHediffOfDef(ShadowCloakDefOf.Raven_Hediff_ShadowStep);
                    if (step != null) step.ageTicks = 0;
                    else caster.health.AddHediff(ShadowCloakDefOf.Raven_Hediff_ShadowStep);
                }

                // 3. 强制打断当前的攻击 Job (防止自动连击)
                if (caster.CurJobDef == JobDefOf.AttackMelee || caster.CurJobDef == JobDefOf.AttackStatic)
                {
                    caster.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                }

                // 4. [核心] 强制重置姿态为 Mobile (消除攻击后摇，允许立即移动)
                if (caster.stances != null)
                {
                    caster.stances.SetStance(new Stance_Mobile());
                }
            }
        }
    }

    // 补丁2：拦截攻击尝试
    // 如果处于暗影冷却状态，禁止开始新的攻击
    [HarmonyPatch(typeof(Verb), "TryStartCastOn", new System.Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public static class Patch_Verb_TryStartCastOn
    {
        [HarmonyPrefix]
        public static bool Prefix(Verb __instance, LocalTargetInfo castTarg, bool surpriseAttack)
        {
            Pawn caster = __instance.CasterPawn;
            if (caster == null) return true;

            // 如果有冷却 Hediff，禁止攻击
            if (caster.health.hediffSet.HasHediff(ShadowCloakDefOf.Raven_Hediff_ShadowAttackCooldown))
            {
                // 给玩家一个反馈
                if (caster.IsColonistPlayerControlled && caster.IsHashIntervalTick(60))
                {
                    MoteMaker.ThrowText(caster.DrawPos, caster.Map, "冷却中", 2f);
                }
                return false; // 拦截原方法
            }

            return true;
        }
    }
}