using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using RavenRace.Items.Comps;

namespace RavenRace.Items.Harmony
{
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class Patch_PoisonWeapon_Hit
    {
        [HarmonyPostfix]
        public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
        {
            Pawn victim = target.Thing as Pawn;
            Thing weapon = __instance.EquipmentSource;
            if (victim == null || weapon == null) return;

            var comp = weapon.TryGetComp<CompRavenInfusion>();
            if (comp != null && comp.ConsumeCharge())
            {
                if (DefenseDefOf.RavenHediff_AphrodisiacEffect != null)
                {
                    HealthUtility.AdjustSeverity(victim, DefenseDefOf.RavenHediff_AphrodisiacEffect, 0.2f);
                    MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Raven_Mote_Poisoned".Translate(), Color.magenta);
                }
            }
        }
    }
}