using Verse;
using RimWorld;

namespace RavenRace.Features.Espionage.Managers
{
    public static class FactionIntelManager
    {
        public static void DailyTick(WorldComponent_Espionage comp)
        {
            // 1. 渗透增长与暴露衰减
            foreach (var spy in comp.GetAllSpies())
            {
                if (spy.state == SpyState.Infiltrating && spy.targetFaction != null)
                {
                    var data = comp.GetSpyData(spy.targetFaction);
                    if (data != null)
                    {
                        float gain = 0.5f + (spy.statNetwork / 20f);
                        if (data.infiltrationPoints < 40f)
                        {
                            data.infiltrationPoints += gain;
                            if (data.infiltrationPoints > 40f) data.infiltrationPoints = 40f;
                        }
                        if (spy.exposure > 0)
                        {
                            spy.exposure -= 1f + (spy.statInfiltration / 50f);
                            if (spy.exposure < 0) spy.exposure = 0;
                        }
                    }
                }
            }

            // 2. 官员更替
            foreach (var kvp in comp.factionData)
            {
                var data = kvp.Value;
                foreach (var official in data.allOfficials)
                {
                    if (official.isDead)
                    {
                        if (Rand.Chance(0.3f))
                        {
                            official.isDead = false;
                            official.isTurncoat = false;
                            official.isKnown = false;
                            official.relationToPlayer = 0;
                            // [修复] 使用 DefDatabase
                            RulePackDef namer = DefDatabase<RulePackDef>.GetNamed("NamerPersonTribal");
                            official.name = NameGenerator.GenerateName(namer);

                            Messages.Message($"{official.factionRef?.Name ?? "敌方"} 的职位空缺已被填补。", MessageTypeDefOf.NeutralEvent);
                        }
                    }
                }
            }
        }
    }
}