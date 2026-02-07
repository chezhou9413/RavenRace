using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RavenRace.Items.Comps;

namespace RavenRace.Items.Harmony
{
    [StaticConstructorOnStartup]
    public static class Patch_WeaponInfusionInjection
    {
        static Patch_WeaponInfusionInjection()
        {
            InjectComps();
        }

        private static void InjectComps()
        {
            int count = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.IsMeleeWeapon && !def.HasComp(typeof(CompRavenInfusion)))
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    def.comps.Add(new CompProperties_RavenInfusion());
                    count++;
                }
            }
             Log.Message($"[RavenRace] Injected CompRavenInfusion to {count} melee weapons.");
        }
    }
}