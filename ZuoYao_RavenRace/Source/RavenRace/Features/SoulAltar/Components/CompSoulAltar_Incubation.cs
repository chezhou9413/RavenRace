using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;
using RavenRace.Features.Reproduction; // [Added]

namespace RavenRace
{
    public partial class CompSoulAltar
    {
        public List<SoulAltarUpgradeDef> GetPotentialUpgrades()
        {
            List<SoulAltarUpgradeDef> list = new List<SoulAltarUpgradeDef>();

            void Collect(Dictionary<IntVec3, Building_AltarInfuser> dict)
            {
                foreach (var kvp in dict)
                {
                    var infuser = kvp.Value;
                    var up = infuser.GetCurrentUpgrade();
                    if (up != null) list.Add(up);
                }
            }

            Collect(connectedInfusers);
            Collect(connectedInjectors);

            return list;
        }

        public void TryStartIncubation()
        {
            Building_Cradle cradle = parent as Building_Cradle;
            if (cradle == null || cradle.GetDirectlyHeldThings().Count == 0)
            {
                Messages.Message("摇篮为空。", parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Thing eggThing = cradle.GetDirectlyHeldThings()[0];
            // [Change] Comp_SpiritEgg -> CompSpiritEgg
            CompSpiritEgg eggComp = eggThing.TryGetComp<CompSpiritEgg>();
            if (eggComp == null) return;

            ScanNetwork();
            List<SoulAltarUpgradeDef> upgrades = new List<SoulAltarUpgradeDef>();

            void Process(Dictionary<IntVec3, Building_AltarInfuser> dict, float glowScale)
            {
                foreach (var kvp in dict)
                {
                    var infuser = kvp.Value;
                    var up = infuser.GetCurrentUpgrade();
                    if (up != null)
                    {
                        upgrades.Add(up);
                        infuser.GetDirectlyHeldThings().ClearAndDestroyContents();
                        FleckMaker.ThrowLightningGlow(infuser.TrueCenter(), parent.Map, glowScale);
                    }
                    infuser.SetTarget(null);
                }
            }

            Process(connectedInfusers, 1f);
            Process(connectedInjectors, 1.5f);

            eggComp.StartIncubation(upgrades);

            FleckMaker.ThrowLightningGlow(parent.TrueCenter(), parent.Map, 3f);
            SoundDefOf.PsychicPulseGlobal.PlayOneShot(parent);

            Messages.Message("孵化仪式已启动！", parent, MessageTypeDefOf.PositiveEvent);
        }
    }
}