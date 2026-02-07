using System.Linq;
using Verse;
using RimWorld;

// [关键] 命名空间统一
namespace RavenRace.Features.Reproduction
{
    public partial class CompSpiritEgg
    {
        private void ApplyStoredUpgrades(Pawn pawn)
        {
            if (storedUpgradeDefNames == null || storedUpgradeDefNames.Count == 0) return;

            foreach (string defName in storedUpgradeDefNames)
            {
                SoulAltarUpgradeDef def = DefDatabase<SoulAltarUpgradeDef>.GetNamedSilentFail(defName);
                if (def == null) continue;

                ApplySkillGains(pawn, def);
                ApplyTraits(pawn, def);
                ApplyHediffs(pawn, def);
                ApplyStatOffsets(pawn, def);
            }
        }

        private void ApplySkillGains(Pawn pawn, SoulAltarUpgradeDef def)
        {
            if (def.skillGains == null) return;

            foreach (var sk in def.skillGains)
            {
                SkillRecord rec = pawn.skills.GetSkill(sk.skill);
                if (rec != null)
                {
                    // if (rec.TotallyDisabled) continue; //小孩太小了，还没解锁，你跳过了肯定就不继承，注释了就行了

                    if (sk.passion.HasValue && rec.passion < sk.passion.Value)
                    {
                        rec.passion = sk.passion.Value;
                    }

                    rec.Learn(sk.xp, true);
                }
            }
        }

        private void ApplyTraits(Pawn pawn, SoulAltarUpgradeDef def)
        {
            if (def.forcedTraits == null) return;

            foreach (var tDef in def.forcedTraits)
            {
                if (pawn.story != null && !pawn.story.traits.HasTrait(tDef))
                {
                    pawn.story.traits.GainTrait(new Trait(tDef));
                }
            }
        }

        private void ApplyHediffs(Pawn pawn, SoulAltarUpgradeDef def)
        {
            if (def.hediffs == null) return;

            foreach (var hDef in def.hediffs)
            {
                pawn.health.AddHediff(hDef);
            }
        }

        private void ApplyStatOffsets(Pawn pawn, SoulAltarUpgradeDef def)
        {
            if (def.statOffsets == null) return;

            HediffDef hDef = RavenDefOf.Raven_Hediff_SoulAltarBonus;
            if (hDef != null)
            {
                var bonusHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hDef) as Hediff_SoulAltarBonus;
                if (bonusHediff == null)
                {
                    bonusHediff = (Hediff_SoulAltarBonus)pawn.health.AddHediff(hDef);
                }

                if (bonusHediff != null)
                {
                    foreach (var statMod in def.statOffsets)
                    {
                        bonusHediff.AddStat(statMod.stat, statMod.value);
                    }
                }
            }
        }
    }
}